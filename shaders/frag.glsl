#version 330 core

in vec2 ndc;
out vec4 fragColor;

#define PI 3.1415926538;

uniform vec2 resolution;
uniform float time;

uniform bool canvasCheck;

uniform bool showDebugView;
uniform int debugView;

uniform bool shadows;
uniform float shadowBias;
uniform bool vvao;
uniform int voxelTraceSteps;

uniform vec3 camPos;
uniform mat4 view;

uniform sampler3D data;
uniform vec3 dataSize;

uniform sampler3D ambientOcclusionData;
uniform vec3 ambientOcclusionDataSize;
uniform int aoDis;

vec3 Sample(vec3 pos)
{
    vec3 value = texture(data, pos / dataSize).rgb;
    return value;
}

float SampleAO(vec3 pos)
{
    float texValue = texture(ambientOcclusionData, pos / aoDis / ambientOcclusionDataSize).r;
    float cutoff = 0.4;
    return min(1 - texValue, cutoff) / cutoff;
}

vec3 intersectAABB(vec3 eye, vec3 dir, vec3 pos, vec3 size)
{
    vec3 t1 = (pos - eye) / dir;
    vec3 t2 = (pos + size - eye) / dir;
    vec3 tMin = min(t1, t2);
    vec3 tMax = max(t1, t2);
    float t = max(tMin.x, max(tMin.y, tMin.z));
    if (t > 0 && t < 9999) return eye + t * dir;
    else return eye;
}

// check if a coord is within the voxel data or not
bool OutsideCanvas(vec3 coord)
{
    if (coord.x < -1 || coord.x > dataSize.x + 1 || coord.y < -1 || coord.y > dataSize.y + 1 || coord.z < -1 || coord.z > dataSize.z + 1) return true;
    else return false;
}

vec3 VoxelTrace(vec3 eye, vec3 dir, out int steps)
{
    vec3 stepsize;
    vec3 toboundry;

    // init stepsize
    stepsize = 1 / abs(dir);

    // init toboundry
    if (dir.x < 0) toboundry.x = (floor(eye.x / 1) - eye.x) / dir.x;
    else toboundry.x = (floor(eye.x / 1) + 1 - eye.x) / dir.x;
    if (dir.y < 0) toboundry.y = (floor(eye.y / 1) - eye.y) / dir.y;
    else toboundry.y = (floor(eye.y / 1) + 1 - eye.y) / dir.y;
    if (dir.z < 0) toboundry.z = (floor(eye.z / 1) - eye.z) / dir.z;
    else toboundry.z = (floor(eye.z / 1) + 1 - eye.z) / dir.z;
    
    // tracing the grid
    vec3 result;
    vec3 coord = floor(eye);
    while (true)
    {
        // increment step
        if (toboundry.x < toboundry.y)
        {
            if (toboundry.x < toboundry.z)
            {
                toboundry.x += stepsize.x;
                if (dir.x < 0) coord.x -= 1;
                else coord.x += 1;
            }
            else
            {
                toboundry.z += stepsize.z;
                if (dir.z < 0) coord.z -= 1;
                else coord.z += 1;
            }
        }
        else
        {
            if (toboundry.y < toboundry.z)
            {
                toboundry.y += stepsize.y;
                if (dir.y < 0) coord.y -= 1;
                else coord.y += 1;
            }
            else
            {
                toboundry.z += stepsize.z;
                if (dir.z < 0) coord.z -= 1;
                else coord.z += 1;
            }
        }
        steps++;

        bool hit = Sample(coord) != vec3(0);
        bool toofar = steps > voxelTraceSteps;
        bool outside = canvasCheck && OutsideCanvas(coord);
        bool anything = hit || toofar || outside;

        if (hit) result = coord;
        if (toofar || outside) result = vec3(0);
        if (anything) break;
    }

    return result;
}

vec3 VoxelNormal(vec3 coord)
{
    int samples = 3;
    int box = samples * 2 + 1;

    vec3 normal = vec3(0);
    for (int x = -samples; x <= samples; x++)
    {
        for (int y = -samples; y <= samples; y++)
        {
            for (int z = -samples; z <= samples; z++)
            {
                vec3 offset = vec3(x, y, z);
                if (offset != vec3(0) && Sample(coord + offset) != vec3(0)) normal += offset;
            }
        }
    }
    return -normalize(normal);
}

void main()
{
    // define bg color
    vec4 bgc = vec4(0.2, 0.2, 0.2, 1.0);

    // calc uv from ndc
    vec2 uv = ndc * normalize(resolution);

    // camera
    vec3 eye = camPos;
    vec3 dir = (view * vec4(uv * 1, 1, 1)).xyz;

    // offset eye to canvas aabb if possible
    if (canvasCheck) eye = intersectAABB(eye, dir, vec3(0), dataSize);

    // if ray never crossed the canvas aabb, return bg color
    if (OutsideCanvas(eye) && canvasCheck)
    {
        fragColor = bgc;
		return;
    }

    // define variables
    vec3 VoxelCoord;
    vec3 normal;

    // trace ray
    int steps;
    VoxelCoord = VoxelTrace(eye, dir, steps);

    // sample hue
    vec3 albedo = Sample(VoxelCoord);

    // calc normals
    normal = VoxelNormal(VoxelCoord);

    // calc light pos
    vec3 lightdir = vec3(1, 0.6, 1);
    vec3 lightpos = normalize(lightdir * 10000);

    // calc diffuse
    float diffuse = max(0.0, dot(lightpos, normal));

    // calc specular
    float exponent = 64;
    float intensity = 0.3;
    float specular = pow(max(dot(normal, normalize(lightdir + dir)), 0.0), exponent) * intensity;

    // calc shadow
    float shadow = 1;
    if (shadows)
    {
        int sdwsteps;
        vec3 startPos = VoxelCoord + lightdir + (normal * shadowBias);
        vec3 shadowVoxel = VoxelTrace(startPos, lightdir, sdwsteps);
        if (shadowVoxel != vec3(0)) shadow = 0;
    }
    diffuse *= shadow;
    specular *= shadow;
    
    // calc ao
    float ao = 1;
    if (vvao) ao = SampleAO(VoxelCoord);

    // calc shaded
    vec3 shaded = albedo * (diffuse * ao + 0.2) + specular;

    // background color
    if (VoxelCoord == vec3(0))
    {
        fragColor = bgc;
		return;
    }
    
    // debug views
    if (showDebugView)
    {
        if (debugView == 0) fragColor = vec4(normal * 0.5 + 0.5, 1.0);
        if (debugView == 1) fragColor = vec4(bgc.x + steps / float(voxelTraceSteps), bgc.y, bgc.z, 1);
        if (debugView == 2) fragColor = vec4(1) * ao;
        return;
    }

    // return result
    fragColor = vec4(shaded, 1.0);
}