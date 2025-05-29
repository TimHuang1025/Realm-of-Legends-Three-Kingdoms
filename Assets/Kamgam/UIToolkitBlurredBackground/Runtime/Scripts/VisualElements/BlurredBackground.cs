#if UNITY_6000_0_OR_NEWER
using Unity.Properties;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitBlurredBackground
{
    /// <summary>
    /// The blurred background works by adding an additional mesh on top of the default mesh via OnGenerateVisualContent().
    /// </summary>
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class BlurredBackground : VisualElement
    {
        public static Color BackgroundColorDefault = new Color(0, 0, 0, 0);

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<BlurredBackground, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_BlurStrength =
                new UxmlFloatAttributeDescription { name = "Blur-Strength", defaultValue = 15f };

            UxmlEnumAttributeDescription<ShaderQuality> m_BlurQuality =
                new UxmlEnumAttributeDescription<ShaderQuality> { name = "Blur-Quality", defaultValue = ShaderQuality.Medium };

            UxmlIntAttributeDescription m_BlurIterations =
                new UxmlIntAttributeDescription { name = "Blur-Iterations", defaultValue = 1 };

            UxmlEnumAttributeDescription<SquareResolution> m_BlurResolution =
                new UxmlEnumAttributeDescription<SquareResolution> { name = "Blur-Resolution", defaultValue = SquareResolution._512 };

            UxmlColorAttributeDescription m_BlurTint =
                new UxmlColorAttributeDescription { name = "Blur-Tint", defaultValue = new Color(1f, 1f, 1f, 1f) };

            UxmlFloatAttributeDescription m_BlurMeshCornerOverlap =
                new UxmlFloatAttributeDescription { name = "Blur-Mesh-Corner-Overlap", defaultValue = 0.3f };

            UxmlIntAttributeDescription m_BlurMeshCornerSegments =
                new UxmlIntAttributeDescription { name = "Blur-Mesh-Corner-Segments", defaultValue = 12 };

            UxmlColorAttributeDescription m_BackgroundColor =
                new UxmlColorAttributeDescription { name = "Background-Color", defaultValue = BackgroundColorDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bg = ve as BlurredBackground;

                // Delay in edito to avoid "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate" warnings.
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
#endif
                    bg.BlurStrength = m_BlurStrength.GetValueFromBag(bag, cc);
                    bg.BlurQuality = m_BlurQuality.GetValueFromBag(bag, cc);
                    bg.BlurIterations = m_BlurIterations.GetValueFromBag(bag, cc);
                    bg.BlurResolution = m_BlurResolution.GetValueFromBag(bag, cc);
                    bg.BlurTint = m_BlurTint.GetValueFromBag(bag, cc);
                    bg.BlurMeshCornerOverlap = m_BlurMeshCornerOverlap.GetValueFromBag(bag, cc);
                    bg.BlurMeshCornerSegments = m_BlurMeshCornerSegments.GetValueFromBag(bag, cc);
                    bg.BackgroundColor = m_BackgroundColor.GetValueFromBag(bag, cc);
#if UNITY_EDITOR
                };
#endif
            }
        }
#endif

        // Cache to have a value to return in GET if no style is defined.
        [System.NonSerialized]
        private int? _cachedBlurIterations;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Iterations")]
        [CreateProperty]
#endif
        public int BlurIterations
        {
            get
            {
                if (!_cachedBlurIterations.HasValue)
                    _cachedBlurIterations = BlurManager.Instance.Iterations;

                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Iterations, this, _cachedBlurIterations.Value);
            }

            set
            {
                _cachedBlurIterations = value;

                int newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Iterations, this, value);
                if (newValue != BlurManager.Instance.Iterations)
                {
                    if (newValue < 0)
                        newValue = 0;

                    BlurManager.Instance.Iterations = newValue;

                    MarkDirtyRepaint();
                }
            }
        }

        [System.NonSerialized]
        private float? _cachedBlurStrength;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Strength")]
        [CreateProperty]
#endif
        public float BlurStrength
        {
            get
            {
                if (!_cachedBlurStrength.HasValue)
                    _cachedBlurStrength = BlurManager.Instance.Offset;

                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Strength, this, _cachedBlurStrength.Value);
            }

            set
            {
                _cachedBlurStrength = value;

                float newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Strength, this, value);
                if (newValue != BlurManager.Instance.Offset)
                {
                    if (newValue < 0f)
                        newValue = 0f;

                    BlurManager.Instance.Offset = newValue;

                    MarkDirtyRepaint();
                }
            }
        }

        protected Vector2Int _blurResolutionSize = new Vector2Int(512, 512);
        public Vector2Int BlurResolutionSize
        {
            get
            {
                return _blurResolutionSize;
            }

            set
            {

                if (value != _blurResolutionSize)
                {
                    if (value.x < 2 || value.y < 2)
                        value = new Vector2Int(2, 2);

                    BlurManager.Instance.Resolution = value;

                    MarkDirtyRepaint();
                }
            }
        }

        [System.NonSerialized]
        private Vector2Int? _cachedBlurResolution;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Resolution")]
        [CreateProperty]
#endif
        public SquareResolution BlurResolution
        {
            get
            {
                if (!_cachedBlurResolution.HasValue)
                    _cachedBlurResolution = _blurResolutionSize;

                if (customStyle.TryGetValue(BlurredBackgroundStyles.Resolution, out var width))
                {
                    return SquareResolutionsUtils.FromResolution(new Vector2Int(width, width));
                }
                else
                {
                    return SquareResolutionsUtils.FromResolution(_cachedBlurResolution.Value);
                }
            }

            set
            {
                var newResolution = SquareResolutionsUtils.ToResolution(value);
                _cachedBlurResolution = newResolution;

                if (customStyle.TryGetValue(BlurredBackgroundStyles.Resolution, out var newWidth))
                {
                    BlurResolutionSize = new Vector2Int(newWidth, newWidth);
                }
                else
                {
                    BlurResolutionSize = newResolution;
                }
            }
        }

        [System.NonSerialized]
        private ShaderQuality? _cachedShaderQuality;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Quality")]
        [CreateProperty]
#endif
        public ShaderQuality BlurQuality
        {
            get
            {
                if (!_cachedShaderQuality.HasValue)
                    _cachedShaderQuality = BlurManager.Instance.Quality;

                if (customStyle.TryGetValue(BlurredBackgroundStyles.Quality, out var qualityString))
                {
                    return ShaderQualityTools.FromString(qualityString);
                }
                else
                {
                    return _cachedShaderQuality.Value;
                }
            }

            set
            {
                _cachedShaderQuality = value;

                ShaderQuality newValue = value;
                if (customStyle.TryGetValue(BlurredBackgroundStyles.Quality, out var qualityString))
                {
                    newValue = ShaderQualityTools.FromString(qualityString);
                }
                
                if (newValue != BlurManager.Instance.Quality)
                {
                    BlurManager.Instance.Quality = value;
                    MarkDirtyRepaint();
                }
            }
        }

        protected Color _blurTint = new Color(1f, 1f, 1f, 1f);
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Tint")]
        [CreateProperty]
#endif
        public Color BlurTint
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Tint, this, _blurTint);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Tint, this, value);
                if (newValue != _blurTint)
                {
                    _blurTint = newValue;
                    MarkDirtyRepaint();
                }
            }
        }

        protected int _blurMeshCornerSegments = 12;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Mesh-Corner-Segments")]
        [CreateProperty]
#endif
        public int BlurMeshCornerSegments
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerSegments, this, _blurMeshCornerSegments);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerSegments, this, value);
                if (newValue != _blurMeshCornerSegments)
                {
                    if (newValue < 1)
                        newValue = 1;

                    _blurMeshCornerSegments = newValue;
                    MarkDirtyRepaint();
                }
            }
        }

        protected float _blurMeshCornerOverlap = 0.3f;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Mesh-Corner-Overlap")]
        [CreateProperty]
#endif
        public float BlurMeshCornerOverlap
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerOverlap, this, _blurMeshCornerOverlap);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerOverlap, this, value);
                if (newValue != _blurMeshCornerOverlap)
                {
                    if (newValue < 0f)
                        newValue = 0f;

                    _blurMeshCornerOverlap = newValue;
                    MarkDirtyRepaint();
                }
            }
        }

        protected Color _defaultBackgroundColor = BackgroundColorDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Background-Color")]
        [CreateProperty]
#endif
        public Color BackgroundColor
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.BackgroundColor, this, _defaultBackgroundColor);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.BackgroundColor, this, value);
                _defaultBackgroundColor = newValue;
                style.backgroundColor = _defaultBackgroundColor;
            }
        }

        // Mesh Data
        Vertex[] _vertices;
        ushort[] _indices;

        protected VisualElement rootParent;

        public BlurredBackground()
        {
            generateVisualContent = OnGenerateVisualContent;

            RegisterCallback<AttachToPanelEvent>(attach);
            RegisterCallback<DetachFromPanelEvent>(detach);
        }

        void attach(AttachToPanelEvent evt)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
#endif
                BlurManager.Instance.AttachElement(this);
#if UNITY_EDITOR
            };
#endif
        }

        void detach(DetachFromPanelEvent evt)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
#endif
                BlurManager.Instance.DetachElement(this);
#if UNITY_EDITOR
            };
#endif
        }

        public virtual void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // Remember: "generateVisualContent is an addition to the default rendering, it's not a replacement"
            // See: https://forum.unity.com/threads/hp-bars-at-runtime-image-masking-or-fill.1076486/#post-6948578 

            if (BlurManager.Instance == null)
                return;

            // If no blur is required then do not even draw the mesh.
            if (BlurIterations <= 0 || BlurManager.Instance.Offset <= 0f || contentRect.width == 0 || contentRect.height == 0)
                return;

            Rect contentRectAbs = contentRect;

            if (contentRectAbs.width + resolvedStyle.paddingLeft + resolvedStyle.paddingRight < 0.01f || contentRectAbs.height + resolvedStyle.paddingTop + resolvedStyle.paddingBottom < 0.01f)
                return;

            // Clamp content
            if (resolvedStyle.borderLeftWidth < 0) contentRectAbs.xMin -= resolvedStyle.borderLeftWidth;
            if (resolvedStyle.borderRightWidth < 0) contentRectAbs.xMax += resolvedStyle.borderRightWidth;
            if (resolvedStyle.borderTopWidth < 0) contentRectAbs.yMin -= resolvedStyle.borderTopWidth;
            if (resolvedStyle.borderBottomWidth < 0) contentRectAbs.yMax += resolvedStyle.borderBottomWidth;


            // Mesh generation

            // clamp to positive
            float borderLeft = Mathf.Clamp(resolvedStyle.borderLeftWidth, 0, resolvedStyle.width * 0.5f);
            float borderRight = Mathf.Clamp(resolvedStyle.borderRightWidth, 0, resolvedStyle.width * 0.5f);
            float borderTop = Mathf.Clamp(resolvedStyle.borderTopWidth, 0, resolvedStyle.height * 0.5f);
            float borderBottom = Mathf.Clamp(resolvedStyle.borderBottomWidth, 0, resolvedStyle.height * 0.5f);

            float radiusTopLeft = Mathf.Max(0, resolvedStyle.borderTopLeftRadius);
            float radiusTopRight = Mathf.Max(0, resolvedStyle.borderTopRightRadius);
            float radiusBottomLeft = Mathf.Max(0, resolvedStyle.borderBottomLeftRadius);
            float radiusBottomRight = Mathf.Max(0, resolvedStyle.borderBottomRightRadius);

            float paddingLeft = Mathf.Max(0, resolvedStyle.paddingLeft);
            float paddingRight = Mathf.Max(0, resolvedStyle.paddingRight);
            float paddingTop = Mathf.Max(0, resolvedStyle.paddingTop);
            float paddingBottom = Mathf.Max(0, resolvedStyle.paddingBottom);

            contentRectAbs.xMin -= paddingLeft;
            contentRectAbs.xMax += paddingRight;
            contentRectAbs.yMin -= paddingTop;
            contentRectAbs.yMax += paddingBottom;

            // Calc inner rect
            // It only starts to curve on the inside once the radius is > the bigger border width
            Vector2 topLeftCornerSize = new Vector2(
                Mathf.Clamp(radiusTopLeft - borderLeft, 0, resolvedStyle.width * 0.5f - borderLeft),
                Mathf.Clamp(radiusTopLeft - borderTop, 0, resolvedStyle.height * 0.5f - borderTop)
            );

            Vector2 topRightCornerSize = new Vector2(
                Mathf.Clamp(radiusTopRight - borderRight, 0, resolvedStyle.width * 0.5f - borderRight),
                Mathf.Clamp(radiusTopRight - borderTop, 0, resolvedStyle.height * 0.5f - borderTop)
            );

            Vector2 bottomLeftCornerSize = new Vector2(
                Mathf.Clamp(radiusBottomLeft - borderLeft, 0, resolvedStyle.width * 0.5f - borderLeft),
                Mathf.Clamp(radiusBottomLeft - borderBottom, 0, resolvedStyle.height * 0.5f - borderBottom)
            );

            Vector2 bottomRightCornerSize = new Vector2(
                Mathf.Clamp(radiusBottomRight - borderRight, 0, resolvedStyle.width * 0.5f - borderRight),
                Mathf.Clamp(radiusBottomRight - borderBottom, 0, resolvedStyle.height * 0.5f - borderBottom)
            );


            // Calc inner quad with corner radius taken into account
            Vector2 innerTopLeft = new Vector2(contentRectAbs.xMin + topLeftCornerSize.x, contentRectAbs.yMin + topLeftCornerSize.y);
            Vector2 innerTopRight = new Vector2(contentRectAbs.xMax - topRightCornerSize.x, contentRectAbs.yMin + topRightCornerSize.y);
            Vector2 innerBottomLeft = new Vector2(contentRectAbs.xMin + bottomLeftCornerSize.x, contentRectAbs.yMax - bottomLeftCornerSize.y);
            Vector2 innerBottomRight = new Vector2(contentRectAbs.xMax - bottomRightCornerSize.x, contentRectAbs.yMax - bottomRightCornerSize.y);

            int verticesPerCorner = BlurMeshCornerSegments;

            // Calc total number of vertices
            // 4 Vertices for the inner rectangle
            // + verticesPerCorner + 2 for each full corner
            // + 1 for a corner with a radius on one side
            // + 0 vertices for a corner without any border radius
            int numVertices = 4; // <- start value

            // Calc total number of indices
            // 6 Vertices for the inner quad (2 tris)
            // + (verticesPerCorner + 1) * 3 for each full corner
            // + 0 for a corner with a radius on one side
            // + 0 vertices for a corner without any border radius
            // Sides
            // + see below
            int numIndices = 6; // <- start value

            // Top Left Corner
            if (topLeftCornerSize.x > 0 && topLeftCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (topLeftCornerSize.x > 0 || topLeftCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Top Right Corner
            if (topRightCornerSize.x > 0 && topRightCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (topRightCornerSize.x > 0 || topRightCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Bottom Left Corner
            if (bottomLeftCornerSize.x > 0 && bottomLeftCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (bottomLeftCornerSize.x > 0 || bottomLeftCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Bottom Right Corner
            if (bottomRightCornerSize.x > 0 && bottomRightCornerSize.y > 0)
            {
                numVertices += verticesPerCorner + 2;
                numIndices += (verticesPerCorner + 1) * 3;
            }
            else if (bottomRightCornerSize.x > 0 || bottomRightCornerSize.y > 0)
            {
                numVertices += 1;
            }

            // Sides (indices)
            // + 6 for a side where the corners form a rectangle
            // + 3 for a side where the corners form a triangle
            // + 0 for a side between two 0 vertex corners
            // Top
            if (topLeftCornerSize.y > 0 && topRightCornerSize.y > 0)
                numIndices += 6;
            else if (topLeftCornerSize.y > 0 || topRightCornerSize.y > 0)
                numIndices += 3;
            // Right
            if (topRightCornerSize.x > 0 && bottomRightCornerSize.x > 0)
                numIndices += 6;
            else if (topRightCornerSize.x > 0 || bottomRightCornerSize.x > 0)
                numIndices += 3;
            // Bottom
            if (bottomRightCornerSize.y > 0 && bottomLeftCornerSize.y > 0)
                numIndices += 6;
            else if (bottomRightCornerSize.y > 0 || bottomLeftCornerSize.y > 0)
                numIndices += 3;
            // Left
            if (bottomLeftCornerSize.x > 0 && topLeftCornerSize.x > 0)
                numIndices += 6;
            else if (bottomLeftCornerSize.x > 0 || topLeftCornerSize.x > 0)
                numIndices += 3;

            if (_vertices == null || _vertices.Length != numVertices)
            {
                _vertices = new Vertex[numVertices];
                _indices = new ushort[numIndices];
            }

            // keep track of indices
            ushort v = 0;
            ushort i = 0;

            // Center rect
            ushort innerBottomLeftVertex = v;
            _vertices[v++].position = new Vector3(innerBottomLeft.x, innerBottomLeft.y, Vertex.nearZ);
            ushort innerTopLeftVertex = v;
            _vertices[v++].position = new Vector3(innerTopLeft.x, innerTopLeft.y, Vertex.nearZ);
            ushort innerTopRightVertex = v;
            _vertices[v++].position = new Vector3(innerTopRight.x, innerTopRight.y, Vertex.nearZ);
            ushort innerBottomRightVertex = v;
            _vertices[v++].position = new Vector3(innerBottomRight.x, innerBottomRight.y, Vertex.nearZ);
            _indices[i++] = 0;
            _indices[i++] = 1;
            _indices[i++] = 2;
            _indices[i++] = 2;
            _indices[i++] = 3;
            _indices[i++] = 0;

            ushort bottomLeftLeftVertex, bottomLeftBottomVertex, bottomRightRightVertex, bottomRightBottomVertex,
                   topLeftLeftVertex, topLeftTopVertex, topRightTopVertex, topRightRightVertex;

            // We add an overlap to make the new mesh overlap the borders a little to reduce gaps.
            float overlapWidth = BlurMeshCornerOverlap;

            // Sides (indices)
            // + 2 tris for a side where the corners form a rectangle
            // + 1 tri for a side where the corners form a triangle
            // Top
            createSide(topLeftCornerSize, topRightCornerSize, cornerSizeNotZeroY, ref v, ref i, innerTopLeftVertex, innerTopRightVertex,
                new Vector3(innerTopLeft.x, innerTopLeft.y - topLeftCornerSize.y - overlapWidth, Vertex.nearZ),
                new Vector3(innerTopRight.x, innerTopRight.y - topRightCornerSize.y - overlapWidth, Vertex.nearZ),
                out topLeftTopVertex, out topRightTopVertex
                );
            // Right
            createSide(topRightCornerSize, bottomRightCornerSize, cornerSizeNotZeroX, ref v, ref i, innerTopRightVertex, innerBottomRightVertex,
                new Vector3(innerTopRight.x + topRightCornerSize.x + overlapWidth, innerTopRight.y, Vertex.nearZ),
                new Vector3(innerBottomRight.x + bottomRightCornerSize.x + overlapWidth, innerBottomRight.y, Vertex.nearZ),
                out topRightRightVertex, out bottomRightRightVertex
                );
            // Bottom
            createSide(bottomRightCornerSize, bottomLeftCornerSize, cornerSizeNotZeroY, ref v, ref i, innerBottomRightVertex, innerBottomLeftVertex,
                new Vector3(innerBottomRight.x, innerBottomRight.y + bottomRightCornerSize.y + overlapWidth, Vertex.nearZ),
                new Vector3(innerBottomLeft.x, innerBottomLeft.y + bottomLeftCornerSize.y + overlapWidth, Vertex.nearZ),
                out bottomRightBottomVertex, out bottomLeftBottomVertex
                );
            // Left
            createSide(bottomLeftCornerSize, topLeftCornerSize, cornerSizeNotZeroX, ref v, ref i, innerBottomLeftVertex, innerTopLeftVertex,
                new Vector3(innerBottomLeft.x - bottomLeftCornerSize.x - overlapWidth, innerBottomLeft.y, Vertex.nearZ),
                new Vector3(innerTopLeft.x - topLeftCornerSize.x - overlapWidth, innerTopLeft.y, Vertex.nearZ),
                out bottomLeftLeftVertex, out topLeftLeftVertex
                );

            if (verticesPerCorner > 0)
            {
                createCorner(topLeftCornerSize, innerTopLeft, verticesPerCorner, ref v, ref i, innerTopLeftVertex, topLeftLeftVertex, topLeftTopVertex, 2);
                createCorner(topRightCornerSize, innerTopRight, verticesPerCorner, ref v, ref i, innerTopRightVertex, topRightTopVertex, topRightRightVertex, 3);
                createCorner(bottomRightCornerSize, innerBottomRight, verticesPerCorner, ref v, ref i, innerBottomRightVertex, bottomRightRightVertex, bottomRightBottomVertex, 0);
                createCorner(bottomLeftCornerSize, innerBottomLeft, verticesPerCorner, ref v, ref i, innerBottomLeftVertex, bottomLeftBottomVertex, bottomLeftLeftVertex, 1);
            }

            MeshWriteData mwd = mgc.Allocate(_vertices.Length, _indices.Length, BlurManager.Instance.GetBlurredTexture());

            // UVs
            if (rootParent == null)
            {
                rootParent = GetDocumentRoot(this);
            }

            for (int n = 0; n < _vertices.Length; n++)
            {
                _vertices[n].tint = BlurTint;

                var uv = this.LocalToWorld(_vertices[n].position);
                uv.x /= rootParent.worldBound.width;
                uv.y /= rootParent.worldBound.height;
                uv.y = 1f - uv.y;

                _vertices[n].uv = uv;
            }

            mwd.SetAllVertices(_vertices);
            mwd.SetAllIndices(_indices);
        }

        private void createCorner(Vector2 cornerSize, Vector2 innerPos, int verticesPerCorner, ref ushort v, ref ushort i, ushort innerVertex, ushort startVertex, ushort endVertex, int quadrantOffset)
        {
            if (cornerSize.x > 0 && cornerSize.y > 0)
            {
                ushort center = innerVertex;
                ushort last = startVertex;

                float offset = Mathf.PI * 0.5f * quadrantOffset;
                float stepSizeInQuadrant = 1f / (verticesPerCorner + 1) * Mathf.PI * 0.5f;

                for (int c = 1; c < verticesPerCorner + 1; c++)
                {
                    float x = Mathf.Cos(offset + stepSizeInQuadrant * c);
                    float y = Mathf.Sin(offset + stepSizeInQuadrant * c);
                    // We also add an overlap to make the new mesh overlap the borders a little to reduce gaps.
                    float overlapWidth = BlurMeshCornerOverlap;
                    _vertices[v++].position = new Vector3(innerPos.x + x * (cornerSize.x + overlapWidth), innerPos.y + y * (cornerSize.y + overlapWidth), Vertex.nearZ);

                    _indices[i++] = center;
                    _indices[i++] = last;
                    _indices[i++] = (ushort)(v - 1);
                    last = _indices[i - 1];
                }

                // End at the existing vertex
                _indices[i++] = center;
                _indices[i++] = last;
                _indices[i++] = endVertex;
            }
        }

        void createSide(
            Vector2 firstCornerSize, Vector2 secondCornerSize,
            System.Func<Vector2, bool> cornerSizeNotZeroFunc,
            ref ushort v, ref ushort i,
            ushort firstOuterVertex, ushort secondOuterVertex,
            Vector3 newVertexAPos, Vector3 newVertexBPos,
            out ushort newVertexA, out ushort newVertexB)
        {
            newVertexA = 0;
            newVertexB = 0;

            if (cornerSizeNotZeroFunc(firstCornerSize) && cornerSizeNotZeroFunc(secondCornerSize))
            {
                newVertexA = v;
                _vertices[v++].position = newVertexAPos;
                newVertexB = v;
                _vertices[v++].position = newVertexBPos;
                _indices[i++] = newVertexA;
                _indices[i++] = newVertexB;
                _indices[i++] = firstOuterVertex;
                _indices[i++] = newVertexB;
                _indices[i++] = secondOuterVertex;
                _indices[i++] = firstOuterVertex;
            }
            else if (cornerSizeNotZeroFunc(firstCornerSize) || cornerSizeNotZeroFunc(secondCornerSize))
            {
                if (cornerSizeNotZeroFunc(firstCornerSize))
                {
                    newVertexA = v;
                    _vertices[v++].position = newVertexAPos;
                    _indices[i++] = newVertexA;
                    _indices[i++] = secondOuterVertex;
                    _indices[i++] = firstOuterVertex;
                }
                else
                {
                    newVertexB = v;
                    _vertices[v++].position = newVertexBPos;
                    _indices[i++] = newVertexB;
                    _indices[i++] = secondOuterVertex;
                    _indices[i++] = firstOuterVertex;
                }
            }
        }

        bool cornerSizeNotZeroX(Vector2 cornerSize)
        {
            return cornerSize.x > 0;
        }

        bool cornerSizeNotZeroY(Vector2 cornerSize)
        {
            return cornerSize.y > 0;
        }

        public VisualElement GetDocumentRoot(VisualElement ele)
        {
            while (ele.parent != null)
            {
                ele = ele.parent;
            }

            return ele;
        }
    }
}