shader_type canvas_item;

// ----- Godot's standard debander for 3.2.4 and 4.0. -----
// NOTE: Requires GLES3.
// INFO: https://github.com/godotengine/godot/pull/42942/commits/2dbc329704c5e04e98a811eb32c65078c690cd82
// USE: Add shader to ColorRect's material or Viewport Collection's material.
// ----- 

// From http://alex.vlachos.com/graphics/Alex_Vlachos_Advanced_VR_Rendering_GDC2015.pdf
// and https://www.shadertoy.com/view/MslGR8 (5th one starting from the bottom)
// NOTE: `frag_coord` is in pixels (i.e. not normalized UV).

vec3 screen_space_dither(vec2 frag_coord) {
	// Iestyn's RGB dither (7 asm instructions) from Portal 2 X360, slightly modified for VR.
	vec3 dither = vec3(dot(vec2(171.0, 231.0), frag_coord));
	dither.rgb = fract(dither.rgb / vec3(103.0, 71.0, 97.0));
	// Subtract 0.5 to avoid slightly brightening the whole viewport.
	return (dither.rgb - 0.5) / 255.0;
}

void fragment() {
 	// For best results, debanding should be done before tonemapping.
	// Otherwise, we're adding noise to an already-quantized image.
	COLOR.rgb += screen_space_dither(FRAGCOORD.xy);
}