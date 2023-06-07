#version 330 core

layout (location = 0) in vec3 vpos;
out vec2 ndc;

void main(void)
{
    ndc = vec2(vpos.x, vpos.y);
    gl_Position = vec4(vpos, 1.0);
}