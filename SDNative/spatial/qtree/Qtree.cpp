#include "Qtree.h"
#include <algorithm>
#include <unordered_set>

namespace spatial
{
    Qtree::Qtree(int worldSize, int smallestCell)
    {
        WorldSize = worldSize;
        smallestCellSize(smallestCell);
    }

    Qtree::~Qtree()
    {
        delete FrontAlloc;
        delete BackAlloc;
    }

    uint32_t Qtree::totalMemory() const
    {
        uint32_t bytes = sizeof(Qtree);
        bytes += FrontAlloc->totalBytes();
        bytes += BackAlloc->totalBytes();
        bytes += Objects.totalMemory();
        return bytes;
    }

    QtreeNode* Qtree::createRoot() const
    {
        QtreeNode* root = FrontAlloc->alloc<QtreeNode>();
        root->setCoords(0, 0, FullSize / 2);
        return root;
    }

    void Qtree::smallestCellSize(int cellSize)
    {
        SmallestCell = cellSize;
        Levels = 0;
        FullSize = cellSize;
        while (FullSize < WorldSize)
        {
            FullSize *= 2;
            ++Levels;
        }
        rebuild();
    }

    void Qtree::clear()
    {
        Objects.clear();
        Root = createRoot();
        Dbg.clear();
    }

    void Qtree::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        FrontAlloc->reset();
        CurrentSplitThreshold = PendingSplitThreshold;

        Objects.submitPending();
        QtreeNode* root = createRoot();

        for (SpatialObject& o : Objects)
        {
            if (o.active)
            {
                insertAt(Levels, *root, &o);
            }
        }
        Root = root;
    }

    struct Overlaps
    {
        bool NW, NE, SE, SW;
        SPATIAL_FINLINE Overlaps(int quadCenterX, int quadCenterY, int objectX, int objectY,
                                 int objectRadiusX, int objectRadiusY)
        {
            // +---------+   The target rectangle overlaps Left quadrants (NW, SW)
            // | x--|    |
            // |-|--+----|
            // | x--|    |
            // +---------+
            bool overlaps_Left = (objectX-objectRadiusX < quadCenterX);
            // +---------+   The target rectangle overlaps Right quadrants (NE, SE)
            // |    |--x |
            // |----+--|-|
            // |    |--x |
            // +---------+
            bool overlaps_Right = (objectX+objectRadiusX >= quadCenterX);
            // +---------+   The target rectangle overlaps Top quadrants (NW, NE)
            // | x--|-x  |
            // |----+----|
            // |    |    |
            // +---------+
            bool overlaps_Top = (objectY-objectRadiusY < quadCenterY);
            // +---------+   The target rectangle overlaps Bottom quadrants (SW, SE)
            // |    |    |
            // |----+----|
            // | x--|-x  |
            // +---------+
            bool overlaps_Bottom = (objectY+objectRadiusY >= quadCenterY);

            // bitwise combine to get which quadrants we overlap: NW, NE, SE, SW
            NW = overlaps_Top & overlaps_Left;
            NE = overlaps_Top & overlaps_Right;
            SE = overlaps_Bottom & overlaps_Right;
            SW = overlaps_Bottom & overlaps_Left;
        }
    };

    void Qtree::insertAt(int level, QtreeNode& root, SpatialObject* o)
    {
        QtreeNode* cur = &root;
        int ox = o->x, oy = o->y, rx = o->rx, ry = o->ry;
        for (;;)
        {
            // try to select a sub-quadrant, perhaps it's a better match
            if (cur->isBranch())
            {
                Overlaps over { cur->cx, cur->cy, ox, oy, rx, ry };

                // bitwise add booleans to get the number of overlaps
                int overlaps = over.NW + over.NE + over.SE + over.SW;

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (over.NW) { cur = cur->nw(); }
                    else if (over.NE) { cur = cur->ne(); }
                    else if (over.SE) { cur = cur->se(); }
                    else if (over.SW) { cur = cur->sw(); }
                }
                else // target overlaps multiple quadrants, so it has to be inserted into several of them:
                {
                    if (over.NW) { insertAt(level-1, *cur->nw(), o); }
                    if (over.NE) { insertAt(level-1, *cur->ne(), o); }
                    if (over.SE) { insertAt(level-1, *cur->se(), o); }
                    if (over.SW) { insertAt(level-1, *cur->sw(), o); }
                    return;
                }
            }
            else // isLeaf
            {
                insertAtLeaf(level, *cur, o);
                return;
            }
        }
    }

    void Qtree::insertAtLeaf(int level, QtreeNode& leaf, SpatialObject* o)
    {
        if (leaf.size < CurrentSplitThreshold)
        {
            leaf.addObject(*FrontAlloc, o, CurrentSplitThreshold);
        }
        // are we maybe over Threshold and should Subdivide ?
        else if (level > 0)
        {
            const int size = leaf.size;
            SpatialObject** objects = leaf.objects;
            leaf.convertToBranch(*FrontAlloc);

            // and now reinsert all items one by one
            for (int i = 0; i < size; ++i)
            {
                insertAt(level-1, leaf, objects[i]);
            }

            // and now try to insert our object again
            insertAt(level-1, leaf, o);
        }
        else
        {
            // final edge case: if number of objects overwhelms the tree,
            // keep dynamically expanding the objects array
            leaf.addObjectUnbounded(*FrontAlloc, o, CurrentSplitThreshold);
        }
    }

    template<class T> struct SmallStack
    {
        static constexpr int MAX = 2048;
        int next = -1;
        T items[MAX];
        #pragma warning(disable:26495)
        SmallStack() = default;
        explicit SmallStack(const T& node) : next{0} { items[0] = node; }
        SPATIAL_FINLINE void push_back(const T& item) { items[++next] = item; }
        SPATIAL_FINLINE T pop_back() { return items[next--]; }
    };

    void Qtree::collideAll(float timeStep, void* user, CollisionFunc onCollide)
    {
        Collider collider;
        SmallStack<QtreeNode*> stack { Root };
        do
        {
            const QtreeNode& current = *stack.pop_back();
            if (current.isBranch())
            {
                stack.push_back(current.sw());
                stack.push_back(current.se());
                stack.push_back(current.ne());
                stack.push_back(current.nw());
            }
            else
            {
                if (int size = current.size)
                    collider.collideObjects(current.objects, size, user, onCollide);
            }
        }
        while (stack.next >= 0);
    }

    #pragma warning( disable : 6262 )
    int Qtree::findNearby(int* outResults, const SearchOptions& opt) const
    {
        FoundNodes found;
        SmallStack<const QtreeNode*> stack { Root };
        int ox = opt.OriginX;
        int oy = opt.OriginY;
        int orx = opt.SearchRadius;
        int ory = opt.SearchRadius;
        do
        {
            const QtreeNode& current = *stack.pop_back();
            int cx = current.cx, cy = current.cy, cr = current.radius;

            if (current.isBranch())
            {
                Overlaps over { cx,cy, ox,oy,orx,ory };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                found.add(current.objects, current.size, {cx,cy}, cr);
            }
        } while (stack.next >= 0 && found.count != found.MAX);

        if (opt.EnableSearchDebugId)
        {
            DebugFindNearby dfn;
            dfn.Circle = { ox, oy, orx };
            dfn.Rectangle = Rect::fromPointRadius(ox, oy, orx);
            dfn.addCells(found);
            Dbg.setFindNearby(opt.EnableSearchDebugId, std::move(dfn));
        }

        return spatial::findNearby(outResults, opt, found);
    }

    void Qtree::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        char text[128];
        int visibleX = opt.visibleWorldRect.centerX();
        int visibleY = opt.visibleWorldRect.centerY();
        int radiusX  = opt.visibleWorldRect.width() / 2;
        int radiusY  = opt.visibleWorldRect.height() / 2;
        visualizer.drawRect(Root->rect(), Yellow);

        SmallStack<const QtreeNode*> stack { Root };
        do
        {
            const QtreeNode& current = *stack.pop_back();
            visualizer.drawRect(current.rect(), Brown);

            int cx = current.cx, cy = current.cy;
            if (current.isBranch())
            {
                if (opt.nodeText)
                    visualizer.drawText({cx,cy}, current.width(), "BR", Yellow);

                Overlaps over { cx, cy, visibleX, visibleY, radiusX, radiusY };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                if (opt.nodeText)
                {
                    snprintf(text, sizeof(text), "LF n=%d", current.size);
                    visualizer.drawText({cx,cy}, current.width(), text, Yellow);
                }
                int count = current.size;
                SpatialObject** const items = current.objects;
                for (int i = 0; i < count; ++i)
                {
                    const SpatialObject& o = *items[i];
                    if (opt.objectBounds)
                        visualizer.drawRect(o.rect(), VioletBright);
                    if (opt.objectToLeafLines)
                        visualizer.drawLine({cx,cy}, {o.x,o.y}, VioletDim);
                    if (opt.objectText)
                    {
                        snprintf(text, sizeof(text), "o=%d", o.objectId);
                        visualizer.drawText({o.x,o.y}, o.rx*2, text, Blue);
                    }
                }
            }
        } while (stack.next >= 0);

        if (opt.searchDebug)
        {
            Dbg.draw(visualizer);
        }
    }
}
