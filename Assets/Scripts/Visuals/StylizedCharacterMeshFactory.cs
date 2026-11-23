using System.Collections.Generic;
using UnityEngine;

namespace TeksMaster.Visuals
{
    /// <summary>
    /// Builds and caches lightweight, smooth procedural meshes for stylized Option-1
    /// chibi/toy-like characters so dozens of Barangay NPCs and the player share the
    /// same Mesh instances with zero redundant allocations.
    /// </summary>
    public static class StylizedCharacterMeshFactory
    {
        private static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _meshCache.Clear();
        }

        public static Mesh GetHeadMesh()
        {
            return GetOrCreate("StylizedHead", BuildHeadMesh);
        }

        public static Mesh GetTorsoMesh()
        {
            return GetOrCreate("StylizedTorso", BuildTorsoMesh);
        }

        public static Mesh GetHipsMesh()
        {
            return GetOrCreate("StylizedHips", BuildHipsMesh);
        }

        public static Mesh GetSleeveMesh()
        {
            return GetOrCreate("StylizedSleeve", BuildSleeveMesh);
        }

        public static Mesh GetTaperedLimbMesh()
        {
            return GetOrCreate("StylizedTaperedLimb", BuildTaperedLimbMesh);
        }

        public static Mesh GetPantCuffMesh()
        {
            return GetOrCreate("StylizedPantCuff", BuildPantCuffMesh);
        }

        public static Mesh GetSneakerSoleMesh()
        {
            return GetOrCreate("StylizedSneakerSole", BuildSneakerSoleMesh);
        }

        public static Mesh GetSneakerUpperMesh()
        {
            return GetOrCreate("StylizedSneakerUpper", BuildSneakerUpperMesh);
        }

        public static Mesh GetHairCapMesh()
        {
            return GetOrCreate("StylizedHairCap", BuildHairCapMesh);
        }

        public static Mesh GetHairTuftMesh()
        {
            return GetOrCreate("StylizedHairTuft", BuildHairTuftMesh);
        }

        public static Mesh GetCurvedArcMesh()
        {
            return GetOrCreate("StylizedCurvedArc", BuildCurvedArcMesh);
        }

        public static Mesh GetSmoothSphereMesh()
        {
            return GetOrCreate("StylizedSmoothSphere", BuildSmoothSphereMesh);
        }

        private static Mesh GetOrCreate(string key, System.Func<Mesh> builder)
        {
            if (_meshCache.TryGetValue(key, out Mesh cached) && cached != null)
                return cached;

            Mesh mesh = builder();
            mesh.name = key;
            _meshCache[key] = mesh;
            return mesh;
        }

        /// <summary>
        /// Sculpted chibi head: softly fuller cheeks, gentle chin taper, smooth forehead.
        /// Unit radius ~0.5 (diameter ~1.0) so localScale controls final meters cleanly.
        /// </summary>
        private static Mesh BuildHeadMesh()
        {
            const int latSteps = 18;
            const int lonSteps = 24;
            var verts = new List<Vector3>((latSteps + 1) * (lonSteps + 1));
            var uvs = new List<Vector2>((latSteps + 1) * (lonSteps + 1));
            var tris = new List<int>(latSteps * lonSteps * 6);

            for (int lat = 0; lat <= latSteps; lat++)
            {
                float v = (float)lat / latSteps;
                float phi = v * Mathf.PI; // 0 = top pole, PI = bottom pole
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                // Subtle chibi cheek fullness around lower-mid face (v ~ 0.58 - 0.72)
                float cheekBulge = 1f + 0.065f * Mathf.Exp(-Mathf.Pow((v - 0.62f) / 0.16f, 2f));
                // Slight chin narrowing near bottom
                float chinPinch = 1f - 0.08f * Mathf.Clamp01((v - 0.72f) / 0.28f);

                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float u = (float)lon / lonSteps;
                    float theta = u * Mathf.PI * 2f;
                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    float x = cosTheta * sinPhi * 0.50f * cheekBulge * chinPinch;
                    float y = cosPhi * 0.48f;
                    float z = sinTheta * sinPhi * 0.47f * chinPinch;

                    // Softly flatten the back-bottom and gently push lower face forward
                    if (z > 0f)
                    {
                        z *= 1f + 0.03f * Mathf.Exp(-Mathf.Pow((v - 0.60f) / 0.20f, 2f));
                    }

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, 1f - v));
                }
            }

            BuildGridTriangles(tris, latSteps, lonSteps);
            return FinalizeMesh(verts, uvs, tris);
        }

        /// <summary>
        /// Smooth rounded t-shirt torso along Y in [0, 1], centered in XZ.
        /// Rounded shoulders at top, gently flared clean hem at bottom.
        /// </summary>
        private static Mesh BuildTorsoMesh()
        {
            // (y, rx, rz) profile rings from bottom hem (0) to collar top (1)
            Vector3[] profile =
            {
                new Vector3(0.00f, 0.001f, 0.001f),
                new Vector3(0.01f, 0.46f, 0.34f),
                new Vector3(0.08f, 0.49f, 0.36f),
                new Vector3(0.28f, 0.46f, 0.35f),
                new Vector3(0.55f, 0.47f, 0.36f),
                new Vector3(0.78f, 0.49f, 0.35f),
                new Vector3(0.90f, 0.43f, 0.30f),
                new Vector3(0.97f, 0.27f, 0.22f),
                new Vector3(1.00f, 0.001f, 0.001f),
            };
            return BuildLoftedEllipticalMesh(profile, 20);
        }

        /// <summary>
        /// Smooth rounded pelvis/hips block centered at origin, unit extents ~1.
        /// </summary>
        private static Mesh BuildHipsMesh()
        {
            Vector3[] profile =
            {
                new Vector3(-0.50f, 0.001f, 0.001f),
                new Vector3(-0.44f, 0.36f, 0.28f),
                new Vector3(-0.22f, 0.48f, 0.36f),
                new Vector3( 0.05f, 0.50f, 0.37f),
                new Vector3( 0.34f, 0.48f, 0.36f),
                new Vector3( 0.46f, 0.38f, 0.28f),
                new Vector3( 0.50f, 0.001f, 0.001f),
            };
            return BuildLoftedEllipticalMesh(profile, 20);
        }

        /// <summary>
        /// Flared short t-shirt sleeve hanging from Y=0 down to Y=-1, rounded shoulder cap.
        /// </summary>
        private static Mesh BuildSleeveMesh()
        {
            Vector3[] profile =
            {
                new Vector3(-1.00f, 0.001f, 0.001f),
                new Vector3(-0.98f, 0.50f, 0.50f),
                new Vector3(-0.80f, 0.48f, 0.48f),
                new Vector3(-0.35f, 0.44f, 0.44f),
                new Vector3(-0.10f, 0.43f, 0.43f),
                new Vector3( 0.06f, 0.35f, 0.35f),
                new Vector3( 0.14f, 0.001f, 0.001f),
            };
            return BuildLoftedEllipticalMesh(profile, 16);
        }

        /// <summary>
        /// Smooth tapered limb capsule along Y in [-0.5, 0.5], slightly wider at top than bottom.
        /// </summary>
        private static Mesh BuildTaperedLimbMesh()
        {
            Vector3[] profile =
            {
                new Vector3(-0.50f, 0.001f, 0.001f),
                new Vector3(-0.46f, 0.30f, 0.30f),
                new Vector3(-0.38f, 0.43f, 0.43f),
                new Vector3(-0.15f, 0.45f, 0.45f),
                new Vector3( 0.15f, 0.48f, 0.48f),
                new Vector3( 0.38f, 0.49f, 0.49f),
                new Vector3( 0.46f, 0.36f, 0.36f),
                new Vector3( 0.50f, 0.001f, 0.001f),
            };
            return BuildLoftedEllipticalMesh(profile, 16);
        }

        /// <summary>
        /// Chunky rolled denim pant cuff ring centered at origin (Y in [-0.5, 0.5]).
        /// </summary>
        private static Mesh BuildPantCuffMesh()
        {
            Vector3[] profile =
            {
                new Vector3(-0.50f, 0.001f, 0.001f),
                new Vector3(-0.46f, 0.46f, 0.46f),
                new Vector3(-0.25f, 0.52f, 0.52f),
                new Vector3( 0.25f, 0.52f, 0.52f),
                new Vector3( 0.46f, 0.46f, 0.46f),
                new Vector3( 0.50f, 0.001f, 0.001f),
            };
            return BuildLoftedEllipticalMesh(profile, 16);
        }

        /// <summary>
        /// Rounded sneaker rubber sole: flat bottom at Y=-0.5, pill-shaped in XZ (longer along +Z).
        /// </summary>
        private static Mesh BuildSneakerSoleMesh()
        {
            Vector3[] profile =
            {
                new Vector3(-0.50f, 0.001f, 0.001f),
                new Vector3(-0.48f, 0.47f, 0.48f),
                new Vector3(-0.15f, 0.50f, 0.50f),
                new Vector3( 0.35f, 0.50f, 0.50f),
                new Vector3( 0.48f, 0.45f, 0.46f),
                new Vector3( 0.50f, 0.001f, 0.001f),
            };
            return BuildLoftedEllipticalMesh(profile, 18);
        }

        /// <summary>
        /// Domed toy sneaker upper: flat bottom at Y=0, rounded toe front (+Z), higher ankle heel (-Z).
        /// </summary>
        private static Mesh BuildSneakerUpperMesh()
        {
            const int rings = 8;
            const int radial = 18;
            var verts = new List<Vector3>((rings + 1) * (radial + 1));
            var uvs = new List<Vector2>((rings + 1) * (radial + 1));
            var tris = new List<int>(rings * radial * 6);

            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings; // 0 = bottom edge, 1 = top apex
                float angle = v * Mathf.PI * 0.5f;
                float radiusScale = Mathf.Cos(angle);
                float baseHeight = Mathf.Sin(angle);

                for (int i = 0; i <= radial; i++)
                {
                    float u = (float)i / radial;
                    float theta = u * Mathf.PI * 2f;
                    float cx = Mathf.Cos(theta);
                    float sz = Mathf.Sin(theta);

                    // Wider at toe box (+Z), slightly higher at ankle (-Z)
                    float toeWiden = sz > 0f ? 1.04f : 0.92f;
                    float ankleBoost = sz < 0f ? (1f + 0.30f * (-sz)) : (1f - 0.16f * sz);

                    float x = cx * 0.48f * radiusScale * toeWiden;
                    float y = baseHeight * 0.85f * ankleBoost;
                    float z = sz * 0.50f * radiusScale;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, v));
                }
            }

            // Note: v goes bottom->top so wind triangles outward (+X normal at theta=0)
            for (int r = 0; r < rings; r++)
            {
                for (int i = 0; i < radial; i++)
                {
                    int row0 = r * (radial + 1);
                    int row1 = (r + 1) * (radial + 1);

                    int a = row0 + i;
                    int b = row0 + i + 1;
                    int c = row1 + i;
                    int d = row1 + i + 1;

                    tris.Add(a);
                    tris.Add(c);
                    tris.Add(b);

                    tris.Add(b);
                    tris.Add(c);
                    tris.Add(d);
                }
            }

            return FinalizeMesh(verts, uvs, tris);
        }

        /// <summary>
        /// Close-fitting sculpted hair cap wrapping upper & back scalp with curved hairline.
        /// </summary>
        private static Mesh BuildHairCapMesh()
        {
            const int latSteps = 14;
            const int lonSteps = 24;
            var verts = new List<Vector3>((latSteps + 1) * (lonSteps + 1));
            var uvs = new List<Vector2>((latSteps + 1) * (lonSteps + 1));
            var tris = new List<int>(latSteps * lonSteps * 6);

            for (int lat = 0; lat <= latSteps; lat++)
            {
                float v = (float)lat / latSteps;
                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float u = (float)lon / lonSteps;
                    float theta = u * Mathf.PI * 2f;
                    float cosT = Mathf.Cos(theta);
                    float sinT = Mathf.Sin(theta);

                    // Hair reaches lower at the back (sinT < 0) and higher at the forehead (sinT > 0)
                    float maxPhi = Mathf.Lerp(Mathf.PI * 0.72f, Mathf.PI * 0.42f, (sinT + 1f) * 0.5f);
                    float phi = v * maxPhi;

                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    float x = cosT * sinPhi * 0.525f;
                    float y = cosPhi * 0.505f;
                    float z = sinT * sinPhi * 0.505f;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, 1f - v));
                }
            }

            BuildGridTriangles(tris, latSteps, lonSteps);
            return FinalizeMesh(verts, uvs, tris);
        }

        /// <summary>
        /// Chunky sculpted stylized hair tuft/spike curving gently forward (+Z) and tapering to a soft point.
        /// Base sits at origin (0,0,0), extends along +Z from 0 to 1 with upward arch.
        /// </summary>
        private static Mesh BuildHairTuftMesh()
        {
            const int lengthSteps = 7;
            const int radial = 10;
            var verts = new List<Vector3>((lengthSteps + 1) * (radial + 1));
            var uvs = new List<Vector2>((lengthSteps + 1) * (radial + 1));
            var tris = new List<int>(lengthSteps * radial * 6);

            for (int s = 0; s <= lengthSteps; s++)
            {
                float t = (float)s / lengthSteps; // 0 = root, 1 = tip
                // Soft plump bulge near base, tapering smoothly to a stylized point
                float width = Mathf.Sin(Mathf.Pow(t, 0.55f) * Mathf.PI) * 0.42f;
                if (t < 0.08f) width = Mathf.Lerp(0.08f, width, t / 0.08f);
                if (t > 0.96f) width = 0.002f;
                float height = width * 0.72f;

                // Gentle organic arch along +Z and slight dip at tip
                float centerZ = t * 1.0f;
                float centerY = Mathf.Sin(t * Mathf.PI) * 0.16f - t * t * 0.08f;

                for (int i = 0; i <= radial; i++)
                {
                    float u = (float)i / radial;
                    float theta = u * Mathf.PI * 2f;

                    float x = Mathf.Cos(theta) * width;
                    float y = centerY + Mathf.Sin(theta) * height;
                    float z = centerZ;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, t));
                }
            }

            // Wind outward along +Z (at theta=0, b-a=+Y, c-a=+Z -> Cross(+Y,+Z)=+X)
            for (int s = 0; s < lengthSteps; s++)
            {
                for (int i = 0; i < radial; i++)
                {
                    int row0 = s * (radial + 1);
                    int row1 = (s + 1) * (radial + 1);

                    int a = row0 + i;
                    int b = row0 + i + 1;
                    int c = row1 + i;
                    int d = row1 + i + 1;

                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(c);

                    tris.Add(b);
                    tris.Add(d);
                    tris.Add(c);
                }
            }

            return FinalizeMesh(verts, uvs, tris);
        }

        /// <summary>
        /// Smooth 3D curved tube arc (arched upward in +Y, spanning X in [-0.5, 0.5]).
        /// Used for curved eyebrows (upright) and smiles (flipped in Y).
        /// </summary>
        private static Mesh BuildCurvedArcMesh()
        {
            const int segs = 10;
            const int radial = 8;
            const float tubeRadius = 0.14f;
            var verts = new List<Vector3>((segs + 1) * (radial + 1));
            var uvs = new List<Vector2>((segs + 1) * (radial + 1));
            var tris = new List<int>(segs * radial * 6);

            for (int s = 0; s <= segs; s++)
            {
                float t = (float)s / segs;
                float x = Mathf.Lerp(-0.5f, 0.5f, t);
                float nx = (t - 0.5f) * 2f; // -1 to 1
                float y = (1f - nx * nx) * 0.24f;

                // Derivative dy/dx = -0.96 * nx
                Vector3 tangent = new Vector3(1f, -0.96f * nx, 0f).normalized;
                Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);
                Vector3 binormal = Vector3.forward;

                // Softly round the ends
                float endPinch = (s == 0 || s == segs) ? 0.15f : (s == 1 || s == segs - 1 ? 0.85f : 1f);
                float r = tubeRadius * endPinch;

                for (int i = 0; i <= radial; i++)
                {
                    float u = (float)i / radial;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 offset = (normal * Mathf.Cos(theta) + binormal * Mathf.Sin(theta)) * r;
                    verts.Add(new Vector3(x, y, 0f) + offset);
                    uvs.Add(new Vector2(t, u));
                }
            }

            for (int s = 0; s < segs; s++)
            {
                for (int i = 0; i < radial; i++)
                {
                    int row0 = s * (radial + 1);
                    int row1 = (s + 1) * (radial + 1);

                    int a = row0 + i;
                    int b = row0 + i + 1;
                    int c = row1 + i;
                    int d = row1 + i + 1;

                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(c);

                    tris.Add(b);
                    tris.Add(d);
                    tris.Add(c);
                }
            }

            return FinalizeMesh(verts, uvs, tris);
        }

        private static Mesh BuildSmoothSphereMesh()
        {
            const int latSteps = 12;
            const int lonSteps = 16;
            var verts = new List<Vector3>((latSteps + 1) * (lonSteps + 1));
            var uvs = new List<Vector2>((latSteps + 1) * (lonSteps + 1));
            var tris = new List<int>(latSteps * lonSteps * 6);

            for (int lat = 0; lat <= latSteps; lat++)
            {
                float v = (float)lat / latSteps;
                float phi = v * Mathf.PI;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float u = (float)lon / lonSteps;
                    float theta = u * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(theta) * sinPhi * 0.5f, cosPhi * 0.5f, Mathf.Sin(theta) * sinPhi * 0.5f));
                    uvs.Add(new Vector2(u, 1f - v));
                }
            }

            BuildGridTriangles(tris, latSteps, lonSteps);
            return FinalizeMesh(verts, uvs, tris);
        }

        private static Mesh BuildLoftedEllipticalMesh(Vector3[] profile, int radialSteps)
        {
            int rings = profile.Length - 1;
            var verts = new List<Vector3>((rings + 1) * (radialSteps + 1));
            var uvs = new List<Vector2>((rings + 1) * (radialSteps + 1));
            var tris = new List<int>(rings * radialSteps * 6);

            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float y = profile[r].x;
                float rx = profile[r].y;
                float rz = profile[r].z;

                for (int i = 0; i <= radialSteps; i++)
                {
                    float u = (float)i / radialSteps;
                    float theta = u * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(theta) * rx, y, Mathf.Sin(theta) * rz));
                    uvs.Add(new Vector2(u, v));
                }
            }

            // Profile increases in Y (bottom -> top): at theta=0, c-a=+Y, b-a=+Z -> Cross(+Y,+Z)=+X
            for (int r = 0; r < rings; r++)
            {
                for (int i = 0; i < radialSteps; i++)
                {
                    int row0 = r * (radialSteps + 1);
                    int row1 = (r + 1) * (radialSteps + 1);

                    int a = row0 + i;
                    int b = row0 + i + 1;
                    int c = row1 + i;
                    int d = row1 + i + 1;

                    tris.Add(a);
                    tris.Add(c);
                    tris.Add(b);

                    tris.Add(b);
                    tris.Add(c);
                    tris.Add(d);
                }
            }

            return FinalizeMesh(verts, uvs, tris);
        }

        private static void BuildGridTriangles(List<int> tris, int latSteps, int lonSteps)
        {
            // lat goes top pole (+Y) -> bottom pole (-Y): at theta=0, b-a=+Z, c-a=-Y -> Cross(+Z,-Y)=+X
            for (int lat = 0; lat < latSteps; lat++)
            {
                for (int lon = 0; lon < lonSteps; lon++)
                {
                    int row0 = lat * (lonSteps + 1);
                    int row1 = (lat + 1) * (lonSteps + 1);

                    int a = row0 + lon;
                    int b = row0 + lon + 1;
                    int c = row1 + lon;
                    int d = row1 + lon + 1;

                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(c);

                    tris.Add(b);
                    tris.Add(d);
                    tris.Add(c);
                }
            }
        }

        private static Mesh FinalizeMesh(List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
