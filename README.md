# Dynamic Lights
This is a mod for Terraria 1.4, meant to add basic dynamic casting of lights and shadows to terraria.

<details>
  <summary>Screenshots</summary>
  
  
</details>

This mod is inspired by Yiyang233's Lights & Shadow mod, just taking it a step further.
It is essentially an adaptation / implementation of Catalin Zima-Zegreanu's shadow algorithm, explained in [this blog post](http://www.catalinzima.com/2010/07/my-technique-for-the-shader-based-dynamic-2d-shadows/)

## Configurable Settings

### Shadow Quality:
Shadow Quality determines the resolution of the rendered shadows.
An increase by one doubles the resolution and thus decreases performance.
Below 8, you might experience inaccuracies or artifacts like very thin objects not casting shadows.
Values above 12 need VERY good GPUs (3070+) with very high VRAM, and will potentially crash your game or system otherwise.

### Brightness Cutoff:
Brightness Cutoff removes lights that are dimmer than the set value from casting shadows.
This helps performance, as every light (including tiny glimmers) that casts shadows needs to be calculated.
For reference, gemspark blocks have a brightness of 0.25 to 0.5 depending on color, and torches a brightness of 1.

### Maximum Light Cap:
Maximum Light Cap limits the amount of lights rendered at the same time.
Will prioritize brighter lights. If you don't want to increase the brightness cutoff or decrease quality and only experience performance issues with a lot of particles, decrease this number.

### Shadow Smoothness:
Shadow Smoothness determines how crisp the shadows are.
The higher the value, the smoother and less pronounced are the shadows.

### Shine Distance:
Shine Distance is a multiplicator for how far the dynamic light is cast from a source.

### Darkest Brightness & Brightest Brightness:
Darkest Brightness determines how much darker areas unlit by dynamic light are.
If set to 0, unlit areas are black. If set to 1, unlit areas are as normal.
Brightest Brightness determines how much brighter lit areas are.
If set to 1, lit areas are as normal, higher values makes them brighter.
Together, these values control the magnitude of the effect.
The farther the values are apart, the stronger the effect.
Special modes are with darkest brightness at 1, which will only make lit areas brighter than normal, and with brightest brightness at 1, which will only make unlit areas darker than normal.

### Brightness Falloff
Brightnes Falloff controls, how very bright lights compare to darker lights.
The closer the value is to 1, the more will brighter light sources appear to be brighter.
The larger the value gets, the more will brighter lightsources appear to light further, instead of brighter.

### Increase Surface Lighting:
Increase Surface Lighting will not dim unlit areas as harshly when the player is in the surface level or above.
This can help alleviate issues with a seemingly dark day when the darkest brightness is set to low values.

### Show Sun:
Show Sun will make the sun cast shadows. This is not recommended unless you have a shadow quality of at least 11, or it will look bad.
