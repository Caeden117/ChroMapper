#ifndef CHROMAPPER_EASINGS_INCLUDED
#define CHROMAPPER_EASINGS_INCLUDED

inline float Step(float k)
{
    return floor(k);
}

inline float Quadratic_In(float k)
{
    return k * k;
}

inline float Quadratic_Out(float k)
{
    return k * (2 - k);
}

inline float Quadratic_InOut(float k)
{
    return (k *= 2) < 1
               ? 0.5 * k * k
               : -0.5 * (((k -= 1) * (k - 2)) - 1);
}

inline float Cubic_In(float k)
{
    return k * k * k;
}

inline float Cubic_Out(float k)
{
    return 1 + ((k -= 1) * k * k);
}

inline float Cubic_InOut(float k)
{
    return (k *= 2) < 1
               ? 0.5 * k * k * k
               : 0.5 * (((k -= 2) * k * k) + 2);
}

inline float Quartic_In(float k)
{
    return k * k * k * k;
}

inline float Quartic_Out(float k)
{
    return 1 - ((k -= 1) * k * k * k);
}

inline float Quartic_InOut(float k)
{
    return (k *= 2) < 1
               ? 0.5 * k * k * k * k
               : -0.5 * (((k -= 2) * k * k * k) - 2);
}

inline float Quintic_In(float k)
{
    return k * k * k * k * k;
}

inline float Quintic_Out(float k)
{
    return 1 + ((k -= 1) * k * k * k * k);
}

inline float Quintic_InOut(float k)
{
    return (k *= 2) < 1
               ? 0.5 * k * k * k * k * k
               : 0.5 * (((k -= 2) * k * k * k * k) + 2);
}

inline float Sinusoidal_In(float k)
{
    return 1 - cos(k * 3.141592654 / 2);
}

inline float Sinusoidal_Out(float k)
{
    return sin(k * 3.141592654 / 2);
}

inline float Sinusoidal_InOut(float k)
{
    return 0.5 * (1 - cos(3.141592654 * k));
}

inline float Exponential_In(float k)
{
    return k == 0
               ? 0
               : pow(1024, k - 1);
}

inline float Exponential_Out(float k)
{
    return k == 1
               ? 1
               : 1 - pow(2, -10 * k);
}

inline float Exponential_InOut(float k)
{
    if (k <= 0 || k >= 1) return k;

    return (k *= 2) < 1
               ? 0.5 * pow(1024, k - 1)
               : 0.5 * (-pow(2, -10 * (k - 1)) + 2);
}

inline float Circular_In(float k)
{
    return 1 - sqrt(1 - (k * k));
}

inline float Circular_Out(float k)
{
    return sqrt(1 - ((k -= 1) * k));
}

inline float Circular_InOut(float k)
{
    return (k *= 2) < 1
               ? -0.5 * (sqrt(1 - (k * k)) - 1)
               : 0.5 * (sqrt(1 - ((k -= 2) * k)) + 1);
}

inline float Elastic_In(float t)
{
    if (t == 0 || t == 1)
    {
        return t;
    }

    return -pow(2, 10 * t - 10) * sin((10 * t - 10.75) * (2 * 3.141592654 / 3));
}

inline float Elastic_Out(float t)
{
    if (t == 0 || t == 1)
    {
        return t;
    }

    return (pow(2, -10 * t) * sin((10 * t - 0.75) * (2 * 3.141592654 / 3))) + 1;
}

inline float Elastic_InOut(float t)
{
    if (t == 0 || t == 1)
    {
        return t;
    }

    if (t < 0.5)
    {
        return -(pow(2, 20 * t - 10) * sin((20 * t - 11.125) * (4 * 3.141592654 / 9))) / 2;
    }

    return (pow(2, -20 * t + 10) * sin((20 * t - 11.125) * (4 * 3.141592654 / 9)) / 2) + 1;
}

// Keep shader constants static; non-static globals can be read as zero.
static const float s = 1.70158;
static const float s2 = 2.5949095;

inline float Back_In(float k)
{
    return k * k * (((s + 1) * k) - s);
}

inline float Back_Out(float k)
{
    return ((k -= 1) * k * (((s + 1) * k) + s)) + 1;
}

inline float Back_InOut(float k)
{
    return (k *= 2) < 1
               ? 0.5 * (k * k * (((s2 + 1) * k) - s2))
               : 0.5 * (((k -= 2) * k * (((s2 + 1) * k) + s2)) + 2);
}

// what the actual fuck.
inline float Bounce_Out(float k)
{
    if (k < 1 / 2.75)
        return 7.5625 * k * k;
    if (k < 2 / 2.75)
        return (7.5625 * (k -= 1.5 / 2.75) * k) + 0.75;
    if (k < 2.5 / 2.75)
        return (7.5625 * (k -= 2.25 / 2.75) * k) + 0.9375;
    return (7.5625 * (k -= 2.625 / 2.75) * k) + 0.984375;
}

inline float Bounce_In(float k)
{
    return 1 - Bounce_Out(1 - k);
}

inline float Bounce_InOut(float k)
{
    return k < 0.5
               ? Bounce_In(k * 2) * 0.5
               : (Bounce_Out((k * 2) - 1) * 0.5) + 0.5;
}

inline float BeatSaberInOutBack(float t)
{
    if (t < 0.517)
        return 5.014 * t * t * t;
    float k = (1.665 * (t - 0.4)) - 1;
    return 1 + (2.70158 * k * k * k) + (1.70158 * k * k);
}

inline float BeatSaberInOutElastic(float t)
{
    if (t < 0.3)
        return 37.037 * t * t * t;
    return (pow(2, -10 * (t - 0.2)) * sin(t * 10 * (2 * 3.141592654 / 3))) + 1;
}

inline float BeatSaberInOutBounce(float t)
{
    if (t < 0.36363637)
        return 20.796 * t * t * t;
    if (t < 0.72727275)
        return (7.5625 * (t - 0.54545456) * (t - 0.54545456)) + 0.75;
    if (t < 0.90909094)
        return (7.5625 * (t - 0.8181818) * (t - 0.8181818)) + 0.9375;
    return (7.5625 * (t - 0.95454545) * (t - 0.95454545)) + 0.984375;
}

float EvaluateEasing(float t, int id)
{
    // Keep these IDs aligned with Easing.EasingShaderId when uploading ribbon easings.
    switch (id)
    {
    // 0 = linear, which is the default at the end
    case 1:
        return Quadratic_In(t);
    case 2:
        return Quadratic_Out(t);
    case 3:
        return Quadratic_InOut(t);
    case 4:
        return Cubic_In(t);
    case 5:
        return Cubic_Out(t);
    case 6:
        return Cubic_InOut(t);
    case 7:
        return Quartic_In(t);
    case 8:
        return Quartic_Out(t);
    case 9:
        return Quartic_InOut(t);
    case 10:
        return Quintic_In(t);
    case 11:
        return Quintic_Out(t);
    case 12:
        return Quintic_InOut(t);
    case 13:
        return Sinusoidal_In(t);
    case 14:
        return Sinusoidal_Out(t);
    case 15:
        return Sinusoidal_InOut(t);
    case 16:
        return Exponential_In(t);
    case 17:
        return Exponential_Out(t);
    case 18:
        return Exponential_InOut(t);
    case 19:
        return Circular_In(t);
    case 20:
        return Circular_Out(t);
    case 21:
        return Circular_InOut(t);
    case 22:
        return Back_In(t);
    case 23:
        return Back_Out(t);
    case 24:
        return Back_InOut(t);
    case 25:
        return Elastic_In(t);
    case 26:
        return Elastic_Out(t);
    case 27:
        return Elastic_InOut(t);
    case 28:
        return Bounce_In(t);
    case 29:
        return Bounce_Out(t);
    case 30:
        return Bounce_InOut(t);
    case 31:
        return Step(t);
    case 32:
        return BeatSaberInOutBack(t);
    case 33:
        return BeatSaberInOutElastic(t);
    case 34:
        return BeatSaberInOutBounce(t);
    default:
        return t; // 0 is linear, but also fall back to anything unknown as linear
    }
}

#endif // CHROMAPPER_EASINGS_INCLUDED
