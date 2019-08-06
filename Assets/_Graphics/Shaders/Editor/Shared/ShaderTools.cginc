// Put this in a folder called "Shared" in the same location as all your shader files.
// Or, put it wherever you want and change the filepath on line 18 of Rainbow.shader.

inline fixed4 RGBtoHSL(fixed4 rgb) {
	fixed4 hsl = fixed4(0.0, 0.0, 0.0, rgb.w);
	
	fixed vMin = min(min(rgb.x, rgb.y), rgb.z);
	fixed vMax = max(max(rgb.x, rgb.y), rgb.z);
	fixed vDelta = vMax - vMin;
	
	hsl.z = (vMax + vMin) / 2.0;
	
	if (vDelta == 0.0) {
		hsl.x = hsl.y = 0.0;
	}
	else {
		if (hsl.z < 0.5) hsl.y = vDelta / (vMax + vMin);
		else hsl.y = vDelta / (2.0 - vMax - vMin);
		
		float rDelta = (((vMax - rgb.x) / 6.0) + (vDelta / 2.0)) / vDelta;
		float gDelta = (((vMax - rgb.y) / 6.0) + (vDelta / 2.0)) / vDelta;
		float bDelta = (((vMax - rgb.z) / 6.0) + (vDelta / 2.0)) / vDelta;
		
		if (rgb.x == vMax) hsl.x = bDelta - gDelta;
		else if (rgb.y == vMax) hsl.x = (1.0 / 3.0) + rDelta - bDelta;
		else if (rgb.z == vMax) hsl.x = (2.0 / 3.0) + gDelta - rDelta;
		
		if (hsl.x < 0.0) hsl.x += 1.0;
		if (hsl.x > 1.0) hsl.x -= 1.0;
	}
	
	return hsl;
}

inline fixed hueToRGB(float v1, float v2, float vH) {
	if (vH < 0.0) vH+= 1.0;
	if (vH > 1.0) vH -= 1.0;
	if ((6.0 * vH) < 1.0) return (v1 + (v2 - v1) * 6.0 * vH);
	if ((2.0 * vH) < 1.0) return (v2);
	if ((3.0 * vH) < 2.0) return (v1 + (v2 - v1) * ((2.0 / 3.0) - vH) * 6.0);
	return v1;
}

inline fixed4 HSLtoRGB(fixed4 hsl) {
	fixed4 rgb = fixed4(0.0, 0.0, 0.0, hsl.w);
	
	if (hsl.y == 0) {
		rgb.xyz = hsl.zzz;
	}
	else {
		float v1;
		float v2;
		
		if (hsl.z < 0.5) v2 = hsl.z * (1 + hsl.y);
		else v2 = (hsl.z + hsl.y) - (hsl.y * hsl.z);
		
		v1 = 2.0 * hsl.z - v2;
		
		rgb.x = hueToRGB(v1, v2, hsl.x + (1.0 / 3.0));
		rgb.y = hueToRGB(v1, v2, hsl.x);
		rgb.z = hueToRGB(v1, v2, hsl.x - (1.0 / 3.0));
	}
	
	return rgb;
}