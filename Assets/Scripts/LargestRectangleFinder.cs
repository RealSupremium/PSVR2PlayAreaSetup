using System;
using System.Collections.Generic;
using Clipper2Lib;

public static class LargestRectangleFinder
{
    /// <summary>
    /// Finds the largest inscribed rectangle aligned to a specific angle.
    /// </summary>
    /// <param name="polygon">The source polygon (PathD is List<PointD>).</param>
    /// <param name="angleRadians">The rotation angle to align the grid to.</param>
    /// <param name="gridResolution">Resolution of the internal search grid (e.g., 60-100).</param>
    /// <returns>A PathD containing the 4 corners of the best rectangle found, or empty if none.</returns>
    public static PathD FindLargestRectAtAngle(PathD polygon, double angleRadians, int gridResolution)
    {
        if (polygon == null || polygon.Count < 3) return new PathD();

        // 1. Rotate Polygon to align with axes (simulate the "angle" view)
        // We rotate by -angle so the target alignment becomes Axis-Aligned.
        PathD rotatedPoly = RotatePath(polygon, -angleRadians);

        // 2. Get Bounding Box of rotated polygon
        RectD bounds = GetBounds(rotatedPoly);
        double width = bounds.right - bounds.left;
        double height = bounds.bottom - bounds.top;

        if (width <= 0 || height <= 0) return new PathD();

        double cellW = width / gridResolution;
        double cellH = height / gridResolution;

        // 3. Fill Grid (0 = outside, 1 = inside)
        // We use a flattened integer array or simple 2D loop.
        // matrix[row, col]
        int[,] matrix = new int[gridResolution, gridResolution];

        for (int r = 0; r < gridResolution; r++)
        {
            double y = bounds.top + (r + 0.5) * cellH;
            for (int c = 0; c < gridResolution; c++)
            {
                double x = bounds.left + (c + 0.5) * cellW;
                PointD pt = new PointD(x, y);

                // Clipper2 PointInPolygon: 
                // Returns PointInPolygonResult.IsInside, .IsOutside, or .IsOn
                PointInPolygonResult result = Clipper.PointInPolygon(pt, rotatedPoly);
                
                if (result != PointInPolygonResult.IsOutside)
                {
                    matrix[r, c] = 1;
                }
                else
                {
                    matrix[r, c] = 0;
                }
            }
        }

        // 4. Largest Rectangle in Histogram Algorithm
        double maxArea = 0;
        RectD bestRectRotatedSpace = new RectD(0, 0, 0, 0);

        // 'heights' stores consecutive vertical counts for the current row
        int[] heights = new int[gridResolution];

        for (int r = 0; r < gridResolution; r++)
        {
            // Update heights for this row
            for (int c = 0; c < gridResolution; c++)
            {
                if (matrix[r, c] == 0)
                    heights[c] = 0;
                else
                    heights[c]++;
            }

            // Calculate max rect in this histogram
            Stack<int> stack = new Stack<int>();
            
            // We loop to gridResolution to force a "flush" of the stack at the end
            for (int i = 0; i <= gridResolution; i++)
            {
                int h = (i == gridResolution) ? 0 : heights[i];

                while (stack.Count > 0 && h < heights[stack.Peek()])
                {
                    int heightVal = heights[stack.Pop()];
                    int widthVal = (stack.Count == 0) ? i : i - stack.Peek() - 1;

                    // Calculate real world area
                    double realW = widthVal * cellW;
                    double realH = heightVal * cellH;
                    double realArea = realW * realH;

                    if (realArea > maxArea)
                    {
                        maxArea = realArea;

                        // Convert grid indices back to rotated world coordinates
                        // Top Row index relative to current 'r' is (r - heightVal + 1)
                        // Left Col index is (i - widthVal)
                        
                        // Note: 'r' is the bottom-most row index of this rect
                        // Clipper2 RectD is (left, top, right, bottom) usually, 
                        // but here we just need x, y, w, h logic.
                        
                        double topY = bounds.top + (r - heightVal + 1) * cellH;
                        double leftX = bounds.left + (i - widthVal) * cellW;

                        bestRectRotatedSpace = new RectD(leftX, topY, leftX + realW, topY + realH);
                    }
                }
                stack.Push(i);
            }
        }

        if (maxArea <= 0) return new PathD();

        // 5. Construct the rectangle in Rotated Space
        PathD rectPoly = new PathD
        {
            new PointD(bestRectRotatedSpace.left, bestRectRotatedSpace.top),     // Top-Left
            new PointD(bestRectRotatedSpace.right, bestRectRotatedSpace.top),    // Top-Right
            new PointD(bestRectRotatedSpace.right, bestRectRotatedSpace.bottom), // Bottom-Right
            new PointD(bestRectRotatedSpace.left, bestRectRotatedSpace.bottom)   // Bottom-Left
        };

        // 6. Rotate back to World Space (+angle)
        return RotatePath(rectPoly, angleRadians);
    }

    // --- Helpers ---

    private static PathD RotatePath(PathD path, double angleRad)
    {
        PathD result = new PathD(path.Count);
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);

        foreach (var p in path)
        {
            double nx = p.x * cos - p.y * sin;
            double ny = p.x * sin + p.y * cos;
            result.Add(new PointD(nx, ny));
        }
        return result;
    }

    private static RectD GetBounds(PathD path)
    {
        if (path.Count == 0) return new RectD(0, 0, 0, 0);
        double minX = path[0].x;
        double maxX = path[0].x;
        double minY = path[0].y;
        double maxY = path[0].y;

        for (int i = 1; i < path.Count; i++)
        {
            if (path[i].x < minX) minX = path[i].x;
            if (path[i].x > maxX) maxX = path[i].x;
            if (path[i].y < minY) minY = path[i].y;
            if (path[i].y > maxY) maxY = path[i].y;
        }

        // Clipper2 RectD structure: left, top, right, bottom
        return new RectD(minX, minY, maxX, maxY);
    }
}