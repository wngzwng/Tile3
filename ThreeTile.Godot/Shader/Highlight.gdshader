shader_type canvas_item;

uniform float begin_time;
uniform bool is_moving;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	float real_time = TIME - begin_time;
	COLOR = texture(TEXTURE, UV);
	COLOR.g += (sin(real_time * float(2.3)) + float(1)) * 0.35;
	if (is_moving) COLOR.r += (sin(real_time * float(2.3)) + float(1)) * 0.35;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}