/*
 * Created by C.J. Kimberlin (http://cjkimberlin.com)
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * 
 * 
 * ============= Description =============
 * 
 * An ColorHSV struct for interpreting a color in hue/saturation/value instead of red/green/blue.
 * NOTE! hue will be a value from 0 to 1 instead of 0 to 360.
 * 
 * ColorHSV hsvRed = new ColorHSV(1, 1, 1, 1); // RED
 * ColorHSV hsvGreen = new ColorHSV(0.333f, 1, 1, 1); // GREEN
 * 
 * 
 * Also supports implicit conversion between Color and Color32.
 * 
 * ColorHSV hsvBlue = Color.blue; // HSVA(0.667f, 1, 1, 1)
 * Color blue = hsvBlue; // RGBA(0, 0, 1, 1)
 * Color32 blue32 = hsvBlue; // RGBA(0, 0, 255, 255) 
 *
 * 
 * If functions are desired instead of implicit conversion then use the following.
 * 
 * Color yellowBefore = Color.yellow; // RBGA(1, .922f, 0.016f, 1)
 * ColorHSV hsvYellow = Color.yellowBefore.ToHSV(); // HSVA(0.153f, 0.984f, 1, 1)
 * Color yellowAfter = hsvYellow.ToRGB(); // RBGA(1, .922f, 0.016f, 1)
 * */

using UnityEngine;

public struct ColorHSV
{
    public float h;
    public float s;
    public float v;
    public float a;

    public ColorHSV(float h, float s, float v, float a)
    {
        this.h = h;
        this.s = s;
        this.v = v;
        this.a = a;
    }

    public override string ToString()
    {
        return string.Format("HSVA: ({0:F3}, {1:F3}, {2:F3}, {3:F3})", h, s, v, a);
    }

    public static implicit operator ColorHSV(Color c)
    {
        return c.ToHSV();
    }

    public static implicit operator Color(ColorHSV hsv)
    {
        return hsv.ToRGB();
    }

    public static implicit operator ColorHSV(Color32 c32)
    {
        return ((Color) c32).ToHSV();
    }

    public static implicit operator Color32(ColorHSV hsv)
    {
        return hsv.ToRGB();
    }

    public override int GetHashCode()
    {
        // This is maybe not a good implementation :)
        return ((Color) this).GetHashCode();
    }

    public Color ToRGB()
    {
        Vector3 rgb = HUEtoRGB(h);
        Vector3 vc = ((rgb - Vector3.one) * s + Vector3.one) * v;

        return new Color(vc.x, vc.y, vc.z, a);
    }

    private static Vector3 HUEtoRGB(float h)
    {
        float r = Mathf.Abs(h * 6 - 3) - 1;
        float g = 2 - Mathf.Abs(h * 6 - 2);
        float b = 2 - Mathf.Abs(h * 6 - 4);

        return new Vector3(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));
    }
}

public static class ColorExtension
{
    private const float EPSILON = 1e-10f;

    public static ColorHSV ToHSV(this Color rgb)
    {
        Vector3 hcv = RGBtoHCV(rgb);
        float s = hcv.y / (hcv.z + EPSILON);

        return new ColorHSV(hcv.x, s, hcv.z, rgb.a);
    }

    private static Vector3 RGBtoHCV(Color rgb)
    {
        Vector4 p = rgb.g < rgb.b ? new Vector4(rgb.b, rgb.g, -1, 2f / 3f) : new Vector4(rgb.g, rgb.b, 0, -1f / 3f);
        Vector4 q = rgb.r < p.x ? new Vector4(p.x, p.y, p.w, rgb.r) : new Vector4(rgb.r, p.y, p.z, p.x);
        float c = q.x - Mathf.Min(q.w, q.y);
        float h = Mathf.Abs((q.w - q.y) / (6 * c + EPSILON) + q.z);

        return new Vector3(h, c, q.x);
    }

    public static Color LerpHsv(this Color a, Color b, float t)
    {
        return LerpHsv((ColorHSV) a, (ColorHSV) b, t);
    }

    public static Color LerpHsv(ColorHSV a, ColorHSV b, float t)
    {
        // Hue interpolation
        float h = 0;
        float d = b.h - a.h;
        if (a.h > b.h)
        {
            // Swap (a.h, b.h)
            var h3 = b.h;
            b.h = a.h;
            a.h = h3;

            d = -d;
            t = 1 - t;
        }

        if (d > 0.5) // 180deg
        {
            a.h = a.h + 1; // 360deg
            h = (a.h + t * (b.h - a.h)) % 1; // 360deg
        }
        if (d <= 0.5) // 180deg
        {
            h = a.h + t * d;
        }

        // Interpolates the rest
        return new ColorHSV
        (
            h, // H
            a.s + t * (b.s - a.s), // S
            a.v + t * (b.v - a.v), // V
            a.a + t * (b.a - a.a) // A
        );
    }
}
