using UnityEngine;

// Helper class for raycasting
public static class RaycastHelper
{
    public static RaycastHit2D GetClosestHit(Vector2 to, RaycastHit2D a, RaycastHit2D b)
    {
        return Vector2.SqrMagnitude(a.point - to) > (double)Vector2.SqrMagnitude(b.point - to) ? b : a;
    }

    public static bool Raycast(Vector2 origin, Vector2 direction, out RaycastHit2D hit)
    {
        return Raycast(origin, direction, out hit, 1f);
    }

    public static bool Raycast(
        Vector2 origin,
        Vector2 direction,
        out RaycastHit2D hit,
        float distance)
    {
        return Raycast(origin, direction, out hit, distance, -1);
    }

    public static bool Raycast(
        Vector2 origin,
        Vector2 direction,
        out RaycastHit2D hit,
        float distance,
        LayerMask layer)
    {
        RaycastHit2D[] raycastHit2DArray = Physics2D.RaycastAll(origin, direction, distance, layer);
        RaycastHit2D b = new RaycastHit2D();

        if (raycastHit2DArray.Length != 0)
        {
            b = raycastHit2DArray[0];

            foreach (RaycastHit2D a in raycastHit2DArray)
            {
                if (!a.collider.isTrigger)
                    b = GetClosestHit(origin, a, b);
            }
        }

        hit = b;
        return b.collider != null;
    }
}