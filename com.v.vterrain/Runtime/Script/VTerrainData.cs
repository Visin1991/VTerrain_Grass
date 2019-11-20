using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace V
{
    [RequireComponent(typeof(Terrain))]
    public class VTerrainData : MonoBehaviour
    {
        [Range(-1.0f, 1.0f)]
        public float BrushIntensity = 0.5f;

        [Range(0.1f,100.0f)]
        public float BrushSize = 10;
          
        [System.Flags]
        public enum PatchSize
        {
            _32 = 1 << 5,
            _64 = 1 << 6,
            _128 = 1 << 7
        }

        [HideInInspector]
        public Vector3 centerPos;

        [HideInInspector]
        public PatchSize patchSize = PatchSize._128;

        private Terrain terrain;
        public Terrain Terrain
        {
            get {
                if (terrain == null) {
                    terrain = GetComponent<Terrain>();
                }
                return terrain;
            }
        }

        private Vector3 size;

        //[HideInInspector]
        public Texture2D m_FinalSource;

        [HideInInspector] public RenderTexture m_TemporaryRT0;
        [HideInInspector] public RenderTexture m_TemporaryRT1;
        [HideInInspector] public RenderTexture m_LastDestination;

        private Material m_CopyColor;
        private Material m_DetailPainter;

        [HideInInspector]
        public Texture2D brushMask;

        public void HitPosition(Vector3 position)
        {
            CheckBrushResources();

            Vector3 localPos = (position - transform.position);
            Vector4 _HitPos = new Vector4(localPos.x / 1024.0f, localPos.z / 1024.0f ,BrushSize/ 1024.0f,BrushIntensity);

            m_DetailPainter.SetVector("_HitPos", _HitPos);
            m_DetailPainter.SetTexture("_Brush", brushMask);

            if (m_LastDestination == m_TemporaryRT0)
            {
                Graphics.Blit(m_TemporaryRT0, m_TemporaryRT1, m_DetailPainter);
                m_LastDestination = m_TemporaryRT1;
            }
            else if(m_LastDestination == m_TemporaryRT1)
            {
                Graphics.Blit(m_TemporaryRT1, m_TemporaryRT0, m_DetailPainter);
                m_LastDestination = m_TemporaryRT0;
            }

            Shader.SetGlobalTexture("_DensityMap", m_LastDestination);
            
        }

        public bool CheckValidation()
        {
            bool result = m_TemporaryRT0 == null;
            result &= m_TemporaryRT1;
            result &= m_LastDestination;
            result &= m_CopyColor;
            result &= m_DetailPainter;
            return result;
        }

        public void CheckBrushResources()
        {

            if (m_CopyColor == null)
            {
                Shader copyShader = Shader.Find("Hidden/V/Copy");
                if (copyShader == null) { return; }
                m_CopyColor = new Material(copyShader);
            }

            if (m_DetailPainter == null)
            {
                Shader detailShader = Shader.Find("Hidden/V/DetailPainter");
                if (detailShader == null) { return; }
                m_DetailPainter = new Material(detailShader);
            }

            if (m_TemporaryRT0 == null || m_TemporaryRT1 == null)
            {
                if (Terrain == null) {
                    return;
                }
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor((int)Terrain.terrainData.size.x, (int)Terrain.terrainData.size.z);
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                m_TemporaryRT0 = RenderTexture.GetTemporary(descriptor);
                m_TemporaryRT1 = RenderTexture.GetTemporary(descriptor);
                Graphics.Blit(m_FinalSource, m_TemporaryRT0, m_CopyColor);
                m_LastDestination = m_TemporaryRT0;
                Shader.SetGlobalTexture("_DensityMap", m_LastDestination);
            } 

        }

        void Create_Patch_Object(int patchNumberX,int patchNumberZ)
        {
            for (int x = 0; x < patchNumberX; x++)
            {
                for (int z = 0; z < patchNumberZ; z++)
                {
                    GameObject obj = new GameObject(x + "____" + z);

                }
            }
        }

        bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public void OnDrawGizmos()
        {
            Color c = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(centerPos, 0.1f * BrushSize);
            Gizmos.color = c;
        }




#if UNITY_EDITOR

        //[Range(1.0f, 128.0f)]
        //public float gridSize = 1.0f;
        //private float gridSize_L;


        //[Range(0.01f, 0.1f)]
        //public float lineSize = 0.1f;
        //private float lineSize_L;

        [HideInInspector]
        public  Mesh brushMesh;
        [HideInInspector]
        public  Material brushMaterial;
        [HideInInspector]
        public  GameObject brushObject;
        [HideInInspector]
        public  List<GameObject> patchObjects;
        [HideInInspector]
        public  Mesh patchMesh;
        [HideInInspector]
        public  Material patchMaterial;
#endif

    }
}
