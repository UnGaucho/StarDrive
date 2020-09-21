#pragma once
#include "Config.h"

namespace spatial
{
    struct Rect
    {
        int left;
        int top;
        int right;
        int bottom;

        // NOTE: No ctor, to avoid sub-optimal code generation

        int centerX() const { return (left + right) >> 1; }
        int centerY() const { return (top + bottom) >> 1; }
        int width()  const { return (right - left); }
        int height() const { return (bottom - top); }

        static Rect fromPointRadius(int x, int y, int r)
        {
            return Rect{x-r, y-r, x+r, y+r};
        }

        SPATIAL_FINLINE bool overlaps(const Rect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
