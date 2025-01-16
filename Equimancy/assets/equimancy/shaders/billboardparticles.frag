#version 330 core

in vec2 uv;
in vec4 colorOut;
in float fogAmount;
in vec4 rgbaFog;

in float glowLevel;

uniform sampler2D tex2d;
uniform vec4 color = vec4(1.0);

layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

#include fogandlight.fsh

void drawPixel(vec4 colorA) {
  float weight =
      colorA.a *
      clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 3e3);

  outAccu = vec4(colorA.rgb * colorA.a, colorA.a) * weight;

  outReveal.r = colorA.a;

  // glowLevel from fogandlight (0-1).

  outGlow = vec4(glowLevel, 0, 0, colorA.a);
}

void main() {
  vec4 color = texture(tex2d, uv) * colorOut;
  drawPixel(applyFogAndShadow(color, fogAmount));
}