/*
holy fuck i am losing my sanity
*/

inline float Linear(float k) {
	return k;
}

inline float Step(float k) {
	return floor(k);
}

inline float Quadratic_In(float k) {
	return k * k;
}

inline float Quadratic_Out(float k) {
	return k * (2 - k);
}

inline float Quadratic_InOut(float k) {
	return (k *= 2) < 1
		? 0.5 * k * k
		: -0.5 * (((k -= 1) * (k - 2)) - 1);
}

inline float Cubic_In(float k) {
	return k * k * k;
}

inline float Cubic_Out(float k) {
	return 1 + ((k -= 1) * k * k);
}

inline float Cubic_InOut(float k) {
	return (k *= 2) < 1
		? 0.5 * k * k * k
		: 0.5 * (((k -= 2) * k * k) + 2);
}

inline float Quartic_In(float k) {
	return k * k * k * k;
}

inline float Quartic_Out(float k) {
	return 1 - ((k -= 1) * k * k * k);
}

inline float Quartic_InOut(float k) {
	return (k *= 2) < 1
		? 0.5 * k * k * k * k
		: -0.5 * (((k -= 2) * k * k * k) - 2);
}

inline float Quintic_In(float k) {
	return k * k * k * k * k;
}

inline float Quintic_Out(float k) {
	return 1 + ((k -= 1) * k * k * k * k);
}

inline float Quintic_InOut(float k) {
	return (k *= 2) < 1
		? 0.5 * k * k * k * k * k
		: 0.5 * (((k -= 2) * k * k * k * k) + 2);
}

inline float Sinusoidal_In(float k) {
	return 1 - cos(k * 3.141592654 / 2);
}

inline float Sinusoidal_Out(float k) {
	return sin(k * 3.141592654 / 2);
}

inline float Sinusoidal_InOut(float k) {
	return 0.5 * (1 - cos(3.141592654 * k));
}

inline float Exponential_In(float k) {
	return k == 0
		? 0
		: pow(1024, k - 1);
}

inline float Exponential_Out(float k) {
	return k == 1
		? 1
		: 1 - pow(2, -10 * k);
}

inline float Exponential_InOut(float k) {
	if (k <= 0 || k >= 1) return k;

	return (k *= 2) < 1
		? 0.5 * pow(1024, k - 1)
		: 0.5 * (-pow(2, -10 * (k - 1)) + 2);
}

inline float Circular_In(float k) {
	return 1 - sqrt(1 - (k * k));
}

inline float Circular_Out(float k) {
	return sqrt(1 - ((k -= 1) * k));
}

inline float Circular_InOut(float k) {
	return (k *= 2) < 1
		? -0.5 * (sqrt(1 - (k * k)) - 1)
		: 0.5 * (sqrt(1 - ((k -= 2) * k)) + 1);
}

inline float Elastic_In(float k) {
	if (k <= 0 || k >= 1) return k;
	return -pow(2, 10 * (k -= 1)) * sin((k - 0.1) * (2 * 3.141592654) / 0.4);
}

inline float Elastic_Out(float k) {
	if (k <= 0 || k >= 1) return k;
	return (pow(2, -10 * k) * sin((k - 0.1) * (2 * 3.141592654) / 0.4f)) + 1;
}

inline float Elastic_InOut(float k) {
	if ((k *= 2) < 1) {
		float val = pow(2, 10 * (k -= 1));
		val *= sin((k - 0.1) * (2 * 3.141592654) / 0.4);
		return val * -0.5;
	}
	else {
		float val = pow(2, -10 * (k -= 1));
		val *= sin((k - 0.1) * (2 * 3.141592654) / 0.4) * 0.5;
		return val + 1;
	}
}

const float s = 1.70158;
const float s2 = 2.5949095;

inline float Back_In(float k) {
	return k * k * (((s + 1) * k) - s);
}

inline float Back_Out(float k) {
	return ((k -= 1) * k * (((s + 1) * k) + s)) + 1;
}

inline float Back_InOut(float k) {
	return (k *= 2) < 1
		? 0.5 * (k * k * (((s2 + 1) * k) - s2))
		: 0.5 * (((k -= 2) * k * (((s2 + 1) * k) + s2)) + 2);
}

// what the actual fuck.
inline float Bounce_Out(float k) {
	if (k < 1 / 2.75)
		return 7.5625 * k * k;
	if (k < 2 / 2.75)
		return (7.5625 * (k -= 1.5 / 2.75) * k) + 0.75;
	if (k < 2.5 / 2.75)
		return (7.5625 * (k -= 2.25 / 2.75) * k) + 0.9375;
	return (7.5625 * (k -= 2.625 / 2.75) * k) + 0.984375;
}

inline float Bounce_In(float k) {
	return 1 - Bounce_Out(1 - k);
}

inline float Bounce_InOut(float k) {
	return k < 0.5
		? Bounce_In(k * 2) * 0.5
		: (Bounce_Out((k * 2) - 1) * 0.5) + 0.5;
}