from pathlib import Path
from PIL import Image, ImageDraw

output = Path(__file__).resolve().parent.parent / "方体.ico"
image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
d = ImageDraw.Draw(image)
d.rounded_rectangle((8, 8, 248, 248), radius=48, fill=(237, 243, 235, 255))
a, b, c, e = (128, 45), (53, 82), (128, 121), (203, 82)
f, g, h = (53, 170), (128, 211), (203, 170)
d.polygon([a, b, c, e], fill=(210, 230, 212))
d.polygon([b, c, g, f], fill=(173, 205, 183))
d.polygon([c, e, h, g], fill=(193, 220, 199))
for p, q in [(a,b),(a,e),(b,c),(c,e),(b,f),(f,g),(c,g),(e,h),(h,g)]:
    d.line((p,q), fill=(47, 83, 62), width=6)
for t in (.25, .5, .75):
    mix = lambda p, q: (p[0]+(q[0]-p[0])*t, p[1]+(q[1]-p[1])*t)
    for p, q, r, s in [(b,c,f,g),(c,e,g,h),(a,b,e,c),(a,e,b,c)]:
        d.line((mix(p,q),mix(r,s)), fill=(93, 132, 105), width=2)
image.save(output, sizes=[(16,16),(32,32),(48,48),(64,64),(128,128),(256,256)])
