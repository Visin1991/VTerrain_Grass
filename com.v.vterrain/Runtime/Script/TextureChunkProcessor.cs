using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class TextureChunkProcessor : MonoBehaviour
{
    
    public Texture2D tex;
    public int indexToDo = 0;

    public Texture2D _8x8Chunk;

    public int numOfX;
    public int numOfY;

    public int indexX;
    public int indexY;

    


    [ContextMenu("Process Index")]
    public void ProcessIndex()
    {
        int x = indexToDo % 8;
        int y = indexToDo / 8;
        DoChunkProcess(tex,x * 8, y * 8);
    }

    [ContextMenu("Process Chunk")]
    public void ProcessChunk()
    {
        DoChunkProcess(_8x8Chunk, 0, 0);
    }

    public void Split_To_8x8()
    {
        int width = tex.width;
        int height = tex.height;
        numOfX = width / 8;
        numOfY = height / 8;
    }



    public void DoChunkProcess(Texture2D _tex, int x = 0,int y=0)
    {
        if (tex == null)
        {
            return;
        }

        Batch batch = new Batch(_tex, x,y);
        batch.ProcessBatch();
    }


    class Batch
    {
        private List<Vector2> final_1x1;
        private List<Vector2> final_2x2;
        private List<Vector2> final_4x4;
        private bool result;

        //
        private bool[] temp_aborted_2x2;
        private bool[] temp_aborted_4x4;
        private bool[] temp_result_2x2;
        private bool[] temp_result_4x4;

    
        private Texture2D chunk;
        private int xOffset;
        private int yOffset;

        public Batch(Texture2D _chunk,int _xOffset,int _yOffset)
        {
            final_1x1 = new List<Vector2>();
            final_2x2 = new List<Vector2>();
            final_4x4 = new List<Vector2>();

            chunk = _chunk;
            xOffset = _xOffset;
            yOffset = _yOffset;
        }

        public void Set_Aborted_2x2(int x, int y, int numOfTrue)
        {
            //If no pixel have value. Then abort the 2x2 chunk
            int key = 4 * y + x;
            temp_aborted_2x2[key] = numOfTrue == 0;
        }

        public bool Get_Aborted_2x2(int x, int y)
        {
            int key = 4 * y + x;
            return temp_aborted_2x2[key];
        }

        public void Set_Aborted_4x4(int x, int y, int numOfTrue)
        {
            //If all 2x2 chunks are aborted. Then we sould abort the 4x4
            int key = 2 * y + x;
            temp_aborted_4x4[key] = numOfTrue ==4;
        }

        public bool Get_Aborted_4x4(int x, int y)
        {
            int key = 2 * y + x;
            return temp_aborted_4x4[key];
        }

        public void Set_Merge_4X4(int x, int y, int numOfValid)
        {
            int key = 2 * y + x;
            temp_result_4x4[key] = numOfValid >= 2;
        }

        public bool Get_Merge_4x4(int x, int y)
        {
            int key = 2 * y + x;
            return temp_result_4x4[key];
        }

        public void Set_Merger_2x2(int x, int y,int numOfTrue)
        {
            int key = 4 * y + x;
            temp_result_2x2[key] = numOfTrue >= 2;
        }

        public bool Get_Merge_2x2(int x, int y)
        {
            int key = 4 * y + x;
            return temp_result_2x2[key];
        }

        public void Add_Valid_Final_4x4()
        {
            for (int i = 0; i < temp_result_4x4.Length; i++)
            {
                Vector2 _4x4Index = Index_To_2D(i);
                if (temp_result_4x4[i])
                {
                    final_4x4.Add(_4x4Index);
                }
                else
                {
                    Add_Valid_Final_2x2(Index_Split_LB(_4x4Index));
                    Add_Valid_Final_2x2(Index_Split_RB(_4x4Index));
                    Add_Valid_Final_2x2(Index_Split_LT(_4x4Index));
                    Add_Valid_Final_2x2(Index_Split_RT(_4x4Index));
                }
            }
        }
        public void Add_Valid_Final_2x2(Vector2 xy)
        {
            int key = (int)(4 * xy.y + xy.x);
            if (temp_result_2x2[key])
            {
                final_2x2.Add(xy);
            }
            else
            {
                Add_Valid_Final_1x1(Index_Split_LB(xy));
                Add_Valid_Final_1x1(Index_Split_RB(xy));
                Add_Valid_Final_1x1(Index_Split_LT(xy));
                Add_Valid_Final_1x1(Index_Split_RT(xy));
            }
        }
        public void Add_Valid_Final_1x1(Vector2 xy)
        {
            Color lb = chunk.GetPixel((int)xy.x + xOffset, (int)xy.y + yOffset);
            if (lb.r > 0) { final_1x1.Add(xy); }
        }

        public List<Vector2> GetFinal_4x4()
        {
            return final_4x4;
        }

        public List<Vector2> GetFinal_2x2()
        {
            return final_2x2;
        }

        public List<Vector2> GetFinal_1x1()
        {
            return final_1x1;
        }

        public bool GetFinal()
        {
            return final_4x4.Count == 4;
        }

        public void ProcessBatch()
        {
            //Allocate the Temporary Data
            temp_result_2x2 = new bool[16];
            temp_result_4x4 = new bool[4];
            temp_aborted_2x2 = new bool[16];
            temp_aborted_4x4 = new bool[4];

            Forward_Check();
            Inverse_Check();
            DebugPrint();

            //Release those Temporary Array
            temp_result_2x2 = null;
            temp_result_4x4 = null;
            temp_aborted_2x2 = null;
            temp_aborted_4x4 = null;

            // TODO 
            //I am not sure should I call this each batch; More likely, I should not 
            //Call this very frequently. So I just Command this out
            //GC.Collect();
        }

        void Forward_Check()
        {

            //Process 2x2 batch
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                   
                    Vector2 posLB = Index_Split_LB(x,y);
                    Vector2 posRB = Index_Split_RB(x,y);
                    Vector2 posLT = Index_Split_LT(x,y);
                    Vector2 posRT = Index_Split_RT(x,y);

                    Color lb = chunk.GetPixel((int)posLB.x + xOffset, (int)posLB.y + yOffset);
                    Color rb = chunk.GetPixel((int)posRB.x + xOffset, (int)posRB.y + yOffset);
                    Color lt = chunk.GetPixel((int)posLT.x + xOffset, (int)posLT.y + yOffset);
                    Color rt = chunk.GetPixel((int)posRT.x + xOffset, (int)posRT.y + yOffset);

                    int numOfTure = 0;
                    if (lb.r > 0) { numOfTure++; }
                    if (rb.r > 0) { numOfTure++; }
                    if (lt.r > 0) { numOfTure++; }
                    if (rt.r > 0) { numOfTure++; }
                    Set_Merger_2x2(x, y, numOfTure);
                    Set_Aborted_2x2(x, y, numOfTure);
                }
            }

            //Process 4x4 batch Set
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    //Check For Valid Value
                    bool lb = Get_Merge_2x2(x * 2 + 0, y * 2 + 0);
                    bool rb = Get_Merge_2x2(x * 2 + 1, y * 2 + 0);
                    bool lt = Get_Merge_2x2(x * 2 + 0, y * 2 + 1);
                    bool rt = Get_Merge_2x2(x * 2 + 1, y * 2 + 1);

                    int numOfValid = 0;
                    if (lb) { numOfValid++; }
                    if (rb) { numOfValid++; }
                    if (lt) { numOfValid++; }
                    if (rt) { numOfValid++; }
                    Set_Merge_4X4(x, y, numOfValid);



                    //Check for Aborted value
                     lb = Get_Aborted_2x2(x * 2 + 0, y * 2 + 0);
                     rb = Get_Aborted_2x2(x * 2 + 1, y * 2 + 0);
                     lt = Get_Aborted_2x2(x * 2 + 0, y * 2 + 1);
                     rt = Get_Aborted_2x2(x * 2 + 1, y * 2 + 1);

                    numOfValid = 0;
                    if (lb) { numOfValid++; }
                    if (rb) { numOfValid++; }
                    if (lt) { numOfValid++; }
                    if (rt) { numOfValid++; }

                    Set_Aborted_4x4(x, y, numOfValid);

                }
            }

            //Process 8x8batch
            {
                int numOfTrue = 0;
                int abortNum = 0;
                for (int i = 0; i < temp_result_4x4.Length; i++)
                {
                    if (temp_result_4x4[i])
                    {
                        numOfTrue++;
                    }
                    if (temp_aborted_4x4[i])
                    {
                        abortNum++;
                    }
                }
                result = (numOfTrue >= 2) && abortNum < 2;
            }
        }

        void Inverse_Check()
        {
            //Final Result for 8x8
            if (result)
            {
                result = true;
                Debug.Log("This should be a 8x8 Chunk");
                return;
            }
            else
            {
                result = false;
            }

            Add_Valid_Final_4x4();
        }

        void DebugPrint()
        {
            for (int i = 0; i < final_4x4.Count; i++)
            {
                Debug.Log("4X4 position : " + final_4x4[i]);
            }

            for (int i = 0; i < final_2x2.Count; i++)
            {
                Debug.Log("2X2 position : " + final_2x2[i]);
            }

            for (int i = 0; i < final_1x1.Count; i++)
            {
                Debug.Log("1X1 position : " + final_1x1[i]);
            }
        }

        static Vector2 Index_To_2D(int index)
        {
            int x = index % 2;
            int y = index / 2;
            return new Vector2(x, y);
        }
        static Vector2 Index_Split_LB(Vector2 index2D)
        {
            return new Vector2(index2D.x * 2 , index2D.y * 2);
        }
        static Vector2 Index_Split_LB(int x,int y)
        {
            return new Vector2(x* 2, y * 2);
        }
        static Vector2 Index_Split_RB(Vector2 index2D)
        {
            return new Vector2(index2D.x * 2 + 1, index2D.y * 2 );
        }
        static Vector2 Index_Split_RB(int x,int y)
        {
            return new Vector2(x * 2 + 1, y * 2);
        }
        static Vector2 Index_Split_LT(Vector2 index2D)
        {
            return new Vector2(index2D.x * 2, index2D.y * 2 + 1);
        }
        static Vector2 Index_Split_LT(int x,int y)
        {
            return new Vector2(x * 2, y * 2 + 1);
        }
        static Vector2 Index_Split_RT(Vector2 index2D)
        {
            return new Vector2(index2D.x * 2 + 1, index2D.y * 2 + 1);
        }
        static Vector2 Index_Split_RT(int x, int y)
        {
            return new Vector2(x * 2 + 1, y * 2 + 1);
        }
    }


}
