﻿using System;

namespace CameraLCD
{
    public struct DisplayId : IEquatable<DisplayId>
    {
        public long EntityId;
        public int SurfaceIndex;

        public DisplayId(long entityId, int surfaceIndex)
        {
            EntityId = entityId;
            SurfaceIndex = surfaceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is DisplayId id && Equals(id);
        }

        public bool Equals(DisplayId other)
        {
            return EntityId == other.EntityId && SurfaceIndex == other.SurfaceIndex;
        }

        public override int GetHashCode()
        {
            int hashCode = -1120748461;
            hashCode = hashCode * -1521134295 + EntityId.GetHashCode();
            hashCode = hashCode * -1521134295 + SurfaceIndex.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(DisplayId left, DisplayId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DisplayId left, DisplayId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return "{EntityId: " + EntityId + ", Area: " + SurfaceIndex + "}";
        }
    }
}
