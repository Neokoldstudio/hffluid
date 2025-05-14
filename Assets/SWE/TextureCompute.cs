using UnityEngine;

public class TextureCompute : MonoBehaviour
{
    public ComputeShader computeShader;
    public Texture2D heightMap;
    public RenderTexture tex1, tex2;
    public Material material;

    public int width = 1024;
    public int height = 1024;

    struct VelocityData
    {
        public float u;
        public float w;
    }

    //---kernels IDs---
    int Advection;
    int HeightIntegration;
    int VelocityIntegration;
    int Boundary;
    int Swap;
    int Init;
    //-----------------

    bool playing = false;

    [SerializeField, Range(0.0f, 1.0f)]
    float deltaTime = 0.001f;

    [SerializeField, Range(0.0f, 10.0f)]
    float dx = 1.0f;

    [SerializeField, Range(0.0f, 1.0f)]
    float epsilon = 0.0001f;

    [SerializeField, Range(0.0f,100.0f)]
    float gravity = 9.81f;

    [SerializeField, Range(0.0f, 10.0f)]
    float alpha = 1.0f;

    [SerializeField, Range(0.0f, 10.0f)]
    float beta = 1.0f;

    [SerializeField, Range(0.0f, 1.0f)]
    float baseHeight = 0.0f;

    void Start()
    {
        InitKernels();
        LoadCompute();
    }

    void Update()
    {
        computeShader.SetFloat("dx", dx);
        computeShader.SetFloat("deltaTime", deltaTime);
        computeShader.SetFloat("g", gravity);
        computeShader.SetFloat("epsilon", epsilon);
        computeShader.SetFloat("alpha", alpha);
        computeShader.SetFloat("beta", beta);

        if (playing)
        {
            SimulationStep();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            playing = !playing;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            playing = false;
            computeShader.Dispatch(Init, width / 8, height / 8, 1);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            playing = false;
            SimulationStep();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            playing = false;
            SimulationStep();
        }
    }

    private void SimulationStep() 
    {
        /*computeShader.Dispatch(Boundary, width / 8, height / 8, 1);
        computeShader.Dispatch(Swap, width / 8, height / 8, 1);*/

        computeShader.Dispatch(Advection, width / 8, height / 8, 1);
        computeShader.Dispatch(Swap, width / 8, height / 8, 1);

        computeShader.Dispatch(HeightIntegration, width / 8, height / 8, 1);
        computeShader.Dispatch(Swap, width / 8, height / 8, 1);

        computeShader.Dispatch(VelocityIntegration, width / 8, height / 8, 1);
        computeShader.Dispatch(Swap, width / 8, height / 8, 1);

    }

    RenderTexture CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        //rt.filterMode = FilterMode.Point;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    void InitKernels()
    {
        HeightIntegration = computeShader.FindKernel("HeightIntegration");
        VelocityIntegration = computeShader.FindKernel("VelocityIntegration");
        Advection = computeShader.FindKernel("VelocityAdvection");
        Boundary = computeShader.FindKernel("BoundaryKernel");
        Swap = computeShader.FindKernel("SwapKernel");
        Init = computeShader.FindKernel("InitKernel");
    }

    void LoadCompute()
    {
        // Initialize render textures
        tex1 = CreateRenderTexture();
        tex2 = CreateRenderTexture();

        computeShader.SetFloat("baseHeight", baseHeight);
        computeShader.SetFloat("dx", dx);
        computeShader.SetFloat("deltaTime", deltaTime);
        computeShader.SetFloat("g", gravity);
        computeShader.SetFloat("epsilon", epsilon);
        computeShader.SetFloat("alpha", alpha);
        computeShader.SetFloat("beta", beta);

        // Shared settings
        computeShader.SetInt("texSizeX", width);
        computeShader.SetInt("texSizeY", height);

        // Set buffers and textures for Init
        computeShader.SetTexture(Init, "Tex1", tex1);
        computeShader.SetTexture(Init, "Tex2", tex2); // If reused
        computeShader.SetTexture(Init, "baseHeightMap", heightMap);

        // Set for HeightIntegration
        computeShader.SetTexture(HeightIntegration, "Tex1", tex1);
        computeShader.SetTexture(HeightIntegration, "Tex2", tex2);

        // Set for VelocityIntegration
        computeShader.SetTexture(VelocityIntegration, "Tex1", tex1);
        computeShader.SetTexture(VelocityIntegration, "Tex2", tex2);

        // Set for Advection
        computeShader.SetTexture(Advection, "Tex1", tex1);
        computeShader.SetTexture(Advection, "Tex2", tex2);

        computeShader.SetTexture(Boundary, "Tex1", tex1);
        computeShader.SetTexture(Boundary, "Tex2", tex2);

        // Swap doesn't use buffers, just the render textures
        computeShader.SetTexture(Swap, "Tex1", tex1);
        computeShader.SetTexture(Swap, "Tex2", tex2);

        // Set to material
        material.SetTexture("_MainTex", tex1);
        material.SetFloat("_Displacement", 0.2f);

        // Dispatch initialization
        computeShader.Dispatch(Init, width / 8, height / 8, 1);
    }
}
