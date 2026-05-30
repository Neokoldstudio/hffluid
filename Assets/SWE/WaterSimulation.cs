using UnityEngine;

public class WaterSimulation : MonoBehaviour
{
    [Header("Inputs")]
    public Texture2D heightMap;
    public Material material;

    [Header("Simulation")]
    public int width = 1024;
    public int height = 1024;

    [SerializeField, Range(0f, 1f)] float deltaTime = 0.001f;
    [SerializeField, Range(0f, 10f)] float dx = 1f;
    [SerializeField, Range(0f, 1f)] float epsilon = 0.0001f;
    [SerializeField, Range(0f, 100f)] float gravity = 9.81f;
    [SerializeField, Range(0f, 10f)] float alpha = 1f;
    [SerializeField, Range(0f, 10f)] float beta = 1f;
    [SerializeField, Range(0f, 1f)] float baseHeight;

    private PingPongTexture textures;
    private bool playing;

    private ComputeShader initShader;
    private ComputeShader advectionShader;
    private ComputeShader heightIntegrationShader;
    private ComputeShader velocityIntegrationShader;
    private ComputeShader boundaryShader;

    private int kernelInit;
    private int kernelAdvection;
    private int kernelHeightIntegration;
    private int kernelVelocityIntegration;
    private int kernelBoundary;

    private ComputeShader[] allShaders;

    private void Start()
    {
        LoadShaders();

        textures = new PingPongTexture(width, height);

        material.SetTexture("_MainTex", textures.Ping);
        material.SetFloat("_Displacement", 0.2f);

        CacheKernelIDs();
        UploadStaticParameters();
        DispatchInit();
    }

    private void LoadShaders()
    {
        const string path = "Compute/";
        initShader = Resources.Load<ComputeShader>(path + "Init");
        advectionShader = Resources.Load<ComputeShader>(path + "Advection");
        heightIntegrationShader = Resources.Load<ComputeShader>(path + "HeightIntegration");
        velocityIntegrationShader = Resources.Load<ComputeShader>(path + "VelocityIntegration");
        boundaryShader = Resources.Load<ComputeShader>(path + "Boundary");

        allShaders = new[] { initShader, advectionShader, heightIntegrationShader, velocityIntegrationShader, boundaryShader };
    }

    private void Update()
    {
        UpdateSimulationParameters();
        material.SetTexture("_MainTex", textures.Ping);

        if (playing)
            SimulationStep();

        if (Input.GetKeyDown(KeyCode.Space))
            playing = !playing;

        if (Input.GetKeyDown(KeyCode.R))
        {
            playing = false;
            DispatchInit();
        }

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.N))
        {
            playing = false;
            SimulationStep();
        }
    }

    private void CacheKernelIDs()
    {
        kernelInit = initShader.FindKernel("InitKernel");
        kernelAdvection = advectionShader.FindKernel("VelocityAdvection");
        kernelHeightIntegration = heightIntegrationShader.FindKernel("HeightIntegration");
        kernelVelocityIntegration = velocityIntegrationShader.FindKernel("VelocityIntegration");
        kernelBoundary = boundaryShader.FindKernel("BoundaryKernel");
    }

    private void UploadStaticParameters()
    {
        float heightMapSize = Mathf.Min(heightMap.width, heightMap.height);

        foreach (var shader in allShaders)
        {
            shader.SetFloat("baseHeightMapSize", heightMapSize);
            shader.SetFloat("baseHeight", baseHeight);
            shader.SetInt("texSizeX", width);
            shader.SetInt("texSizeY", height);
        }
    }

    private void UpdateSimulationParameters()
    {
        foreach (var shader in allShaders)
        {
            shader.SetFloat("dx", dx);
            shader.SetFloat("deltaTime", deltaTime);
            shader.SetFloat("g", gravity);
            shader.SetFloat("epsilon", epsilon);
            shader.SetFloat("alpha", alpha);
            shader.SetFloat("beta", beta);
        }
    }

    private void DispatchInit()
    {
        initShader.SetTexture(kernelInit, "Tex1", textures.Ping);
        initShader.SetTexture(kernelInit, "Tex2", textures.Pong);
        initShader.SetTexture(kernelInit, "baseHeightMap", heightMap);
        initShader.Dispatch(kernelInit, width / 8, height / 8, 1);
    }

    private void SimulationStep()
    {
        DispatchStep(boundaryShader, kernelBoundary);
        textures.Swap();

        DispatchStep(advectionShader, kernelAdvection);
        textures.Swap();

        DispatchStep(heightIntegrationShader, kernelHeightIntegration);
        textures.Swap();

        DispatchStep(velocityIntegrationShader, kernelVelocityIntegration);
        textures.Swap();
    }

    private void DispatchStep(ComputeShader shader, int kernel)
    {
        shader.SetTexture(kernel, "Tex1", textures.Ping);
        shader.SetTexture(kernel, "Tex2", textures.Pong);
        shader.Dispatch(kernel, width / 8, height / 8, 1);
    }

    private void OnDestroy()
    {
        textures?.Release();
    }

    private void ComputeVolume()
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = textures.Ping;

        var readTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        readTex.Apply();

        RenderTexture.active = prev;

        float areaSize = dx * dx;
        float totalVolume = 0f;
        Color[] pixels = readTex.GetPixels();
        foreach (Color c in pixels)
            totalVolume += c.g * areaSize;

        Debug.Log($"Total water volume: {totalVolume}");
    }
}
