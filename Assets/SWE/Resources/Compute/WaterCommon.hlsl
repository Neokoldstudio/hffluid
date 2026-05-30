#define LEFT int2(1,0)
#define RIGHT int2(-1,0)
#define UP int2(0,-1)
#define DOWN int2(0,1)

Texture2D<float4> baseHeightMap;
RWTexture2D<float4> Tex1;
RWTexture2D<float4> Tex2;

float baseHeight;
float baseHeightMapSize;
float texSizeX;
float texSizeY;
float deltaTime;
float dx;
float g;
float epsilon;
float alpha;
float beta;

int2 clampUV(int2 uv)
{
    uv.x = clamp(uv.x, 0, texSizeX);
    uv.y = clamp(uv.y, 0, texSizeY);
    return uv;
}

float getHeight(int2 uv)
{
    return Tex1[clampUV(uv)].r;
}

float getDepth(int2 uv)
{
    return Tex1[clampUV(uv)].g;
}

float getUVel(int2 uv)
{
    return Tex1[clampUV(uv)].b;
}

float getWVel(int2 uv)
{
    return Tex1[clampUV(uv)].a;
}

void ComputeCellVel(int2 uv, out float uR, out float uL, out float wU, out float wD)
{
    uR = getUVel(uv);
    uL = getUVel(uv + LEFT);
    wU = getWVel(uv);
    wD = getWVel(uv + DOWN);
}

void ComputeUpwindHeights(int2 uv, out float hR, out float hL, out float hU, out float hD)
{
    float uR, uL, wU, wD;
    ComputeCellVel(uv, uR, uL, wU, wD);

    float hC = getDepth(uv);

    hL = (uL > 0) ? getDepth(uv + LEFT) : hC;
    hR = (uR > 0) ? hC : getDepth(uv + RIGHT);
    hD = (wD > 0) ? getDepth(uv + DOWN) : hC;
    hU = (wU > 0) ? hC : getDepth(uv + UP);

    float avgmax = beta * (dx / (g * deltaTime));
    float adj = max(0.0, (hL + hR + hD + hU) / 4 - avgmax);

    hL -= adj;
    hR -= adj;
    hD -= adj;
    hU -= adj;
}
