#version 330 core
  
in vec2 ndc;
out vec4 fragColor;

uniform sampler2D fbtex;

void main()
{
    fragColor = texture(fbtex, ndc * 0.5 + 0.5);
}