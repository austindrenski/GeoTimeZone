﻿using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;

namespace GeoTimeZone.DataBuilder
{
    public class GeohashTree : List<GeohashTreeNode>
    {
        public const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
        public const int Precision = 5;

        public GeohashTree()
        {
            AddRange(GetNextLevel());
        }

        public string[] GetGeohashes(Geometry geometry)
        {
            return this.SelectMany(level => GetGeohashes(geometry, level)).ToArray();
        }

        private static IEnumerable<string> GetGeohashes(Geometry geometry, GeohashTreeNode level)
        {
            try
            {
                Geometry env = level.Geometry;

                if (geometry.Contains(env))
                {
                    return new[] {level.Geohash};
                }

                if (!geometry.Intersects(env))
                {
                    return new string[0];
                }

                if (level.Geohash.Length == Precision)
                {
                    return new[] {level.Geohash};
                }

                return level.GetChildren().SelectMany(child => GetGeohashes(geometry, child));
            }
            catch
            {
                // Ignore errors caused by invalid geometry
                return new string[0];
            }
        }

        public GeohashTreeNode GetTreeNode(string geohash)
        {
            if (string.IsNullOrWhiteSpace(geohash))
            {
                return null;
            }

            GeohashTreeNode result = null;
            foreach (char c in geohash)
            {
                if (c == '-')
                {
                    return result;
                }

                int index = Base32.IndexOf(c);
                result = result == null ? this[index] : result.GetChildren()[index];
            }

            return result;
        }

        public static IEnumerable<GeohashTreeNode> GetNextLevel(string geohash = "", Envelope envelope = null)
        {
            if (geohash == string.Empty || envelope == null)
            {
                geohash = "";
                envelope = new Envelope(-180, 180, -90, 90);
            }

            bool even = geohash.Length % 2 == 0;

            return SplitEnvelope2(envelope, even)
                .SelectMany(x => SplitEnvelope4(x, even))
                .SelectMany(x => SplitEnvelope4(x, even))
                .Select((envelope1, index) => new GeohashTreeNode { Envelope = envelope1, Geohash = geohash + Base32[index] });
        }

        public static IEnumerable<Envelope> SplitEnvelope2(Envelope envelope, bool even)
        {
            if (even)
            {

                double midX = envelope.MinX + envelope.Width / 2;
                yield return new Envelope(envelope.MinX, midX, envelope.MinY, envelope.MaxY);
                yield return new Envelope(midX, envelope.MaxX, envelope.MinY, envelope.MaxY);
            }
            else
            {
                double midY = envelope.MinY + envelope.Height / 2;
                yield return new Envelope(envelope.MinX, envelope.MaxX, envelope.MinY, midY);
                yield return new Envelope(envelope.MinX, envelope.MaxX, midY, envelope.MaxY);
            }
        }

        public static IEnumerable<Envelope> SplitEnvelope4(Envelope envelope, bool even)
        {
            double minX = envelope.MinX;
            double minY = envelope.MinY;

            double stepX = envelope.Width / 2;
            double stepY = envelope.Height / 2;

            if (even)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        double x1 = minX + (stepX * x);
                        double y1 = minY + (stepY * y);
                        yield return new Envelope(x1, x1 + stepX, y1, y1 + stepY);
                    }
                }
            }
            else
            {
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        double x1 = minX + (stepX * x);
                        double y1 = minY + (stepY * y);
                        yield return new Envelope(x1, x1 + stepX, y1, y1 + stepY);
                    }
                }
            }
        }
    }
}