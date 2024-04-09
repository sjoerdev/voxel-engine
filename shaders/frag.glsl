#version 330 core

in vec2 ndc;
out vec4 fragColor;

uniform vec2 resolution;
uniform float time;

uniform bool canvasCheck;
uniform bool showDebugView;
uniform int debugView;
uniform bool shadows;
uniform float shadowBias;
uniform bool vvao;
uniform int maxsteps;
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

vec3 HitBoundingBox(vec3 eye, vec3 dir, vec3 pos, vec3 size)
{
    vec3 t1 = (pos - eye) / dir;
    vec3 t2 = (pos + size - eye) / dir;
    vec3 tMin = min(t1, t2);
    vec3 tMax = max(t1, t2);
    float t = max(tMin.x, max(tMin.y, tMin.z));
    if (t > 0 && t < 9999) return eye + t * dir;
    else return eye;
}

bool OutsideCanvas(vec3 coord)
{
    if (coord.x < -1 || coord.x > dataSize.x + 1 || coord.y < -1 || coord.y > dataSize.y + 1 || coord.z < -1 || coord.z > dataSize.z + 1) return true;
    else return false;
}

vec3 VoxelTrace(vec3 eye, vec3 dir, out int steps)
{
    vec3 result;
    vec3 stepsize = 1 / abs(dir);
    vec3 toboundry = (sign(dir) * 0.5 + 0.5 - fract(eye)) / dir;
    vec3 voxel = floor(eye);

    while (true)
    {
        if (toboundry.x < toboundry.y)
        {
            if (toboundry.x < toboundry.z)
            {
                toboundry.x += stepsize.x;
                voxel.x += dir.x > 0 ? 1 : -1;
            }
            else
            {
                toboundry.z += stepsize.z;
                voxel.z += dir.z > 0 ? 1 : -1;
            }
        }
        else
        {
            if (toboundry.y < toboundry.z)
            {
                toboundry.y += stepsize.y;
                voxel.y += dir.y > 0 ? 1 : -1;
            }
            else
            {
                toboundry.z += stepsize.z;
                voxel.z += dir.z > 0 ? 1 : -1;
            }
        }
        steps++;

        bool hit = Sample(voxel) != vec3(0);
        bool toofar = steps > maxsteps;
        bool outside = canvasCheck && OutsideCanvas(voxel);
        bool anything = hit || toofar || outside;

        if (hit) result = voxel;
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

    // offset eye to canvas
    if (canvasCheck) eye = HitBoundingBox(eye, dir, vec3(0), dataSize);

    // if ray never enters canvas return background
    if (OutsideCanvas(eye) && canvasCheck)
    {
        fragColor = bgc;
		return;
    }
    
    // trace ray
    int steps;
    vec3 voxel = VoxelTrace(eye, dir, steps);
    
    // background color
    if (voxel == vec3(0))
    {
        fragColor = bgc;
		return;
    }

    // sample color
    vec3 albedo = Sample(voxel);

    // calc normals
    vec3 normal = VoxelNormal(voxel);

    // define light
    vec3 lightdir = vec3(1, 0.6, 1);
    vec3 lightpos = normalize(lightdir * 10000);

    // calc diffuse
    float diffuse = max(0.0, dot(lightpos, normal));

    // calc specular
    float exponent = 64;
    float intensity = 0.3;
    float specular = pow(max(dot(normal, normalize(lightdir + dir)), 0.0), exponent) * intensity;

    // calc shadows
    if (shadows)
    {
        int sdwsteps;
        vec3 start = voxel + lightdir + (normal * shadowBias);
        vec3 shadowVoxel = VoxelTrace(start, lightdir, sdwsteps);
        if (shadowVoxel != vec3(0))
        {
            diffuse = 0;
            specular = 0;
        }
    }
    
    // calc ao
    float ao = 1;
    if (vvao) ao = SampleAO(voxel);

    // calc shaded
    vec3 shaded = albedo * (diffuse * ao + 0.2) + specular;
    
    // debug views
    if (showDebugView)
    {
        if (debugView == 0) fragColor = vec4(normal * 0.5 + 0.5, 1.0);
        if (debugView == 1) fragColor = vec4(bgc.x + steps / float(maxsteps), bgc.y, bgc.z, 1);
        if (debugView == 2) fragColor = vec4(1) * ao;
        return;
    }

    // return result
    fragColor = vec4(shaded, 1.0);
}