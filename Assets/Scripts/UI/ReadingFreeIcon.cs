using UnityEngine;
using UnityEngine.UI;

namespace Deprem.Accessibility
{
    // UGUI geometry, independent of font coverage. All icons are non-interactive.
    public sealed class ReadingFreeIcon : MaskableGraphic
    {
        [field: SerializeField] public string Kind { get; private set; } = "hand";
        private Texture2D artwork;
        private float progress = 1;
        private int count = 1;
        private bool badge = true;
        private Color inkColor = new Color32(32,62,59,255);
        public void Style(Color ink, bool showBadge)
        {
            if(inkColor==ink && badge==showBadge)return;
            inkColor=ink;badge=showBadge;SetVerticesDirty();
        }
        public override Texture mainTexture => artwork != null ? artwork : s_WhiteTexture;
        public void Set(string kind, float value = 1, int repeat = 1)
        {
            if (Kind == kind && Mathf.Abs(progress - value) < .01f && count == repeat) return;
            Kind = kind; progress = value; count = repeat;
            artwork = kind.StartsWith("item-") || kind.StartsWith("icon-") ? Resources.Load<Texture2D>("LearningApp/Art/" + kind) : null;
            SetAllDirty();
        }
        protected override void Awake()
        {
            base.Awake(); raycastTarget = false;
            artwork = Kind.StartsWith("item-") || Kind.StartsWith("icon-") ? Resources.Load<Texture2D>("LearningApp/Art/" + Kind) : null;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect bounds = GetPixelAdjustedRect();
            if (artwork != null)
            {
                float scale = Mathf.Min(bounds.width / artwork.width, bounds.height / artwork.height);
                Vector2 half = new Vector2(artwork.width, artwork.height) * (scale * .5f);
                Quad(vh, bounds.center-half, bounds.center+half, Color.white); return;
            }
            int repeats = Mathf.Clamp(count, 1, 16);
            float size = Mathf.Min(bounds.height, bounds.width / repeats) * .84f;
            for (int i = 0; i < repeats; i++)
            {
                Vector2 offset = bounds.center + new Vector2((i - (repeats-1)*.5f)*size, 0) - Vector2.one * size*.5f;
                Color ink = inkColor; var gold = new Color32(243,199,149,255);
                Color tint = (i+.5f)/repeats <= progress ? ink : new Color32(167,182,172,255);
                Vector2 V(float x,float y) => offset + new Vector2(x,100-y) * size/100;
                void Line(params float[] points)
                {
                    for(int n=0;n<points.Length-2;n+=2) Segment(vh,V(points[n],points[n+1]),V(points[n+2],points[n+3]),size*.065f,tint);
                }
                void Poly(Color c, params float[] points)
                {
                    Vector2 origin=V(points[0],points[1]);
                    for(int n=2;n<points.Length-2;n+=2) Triangle(vh,origin,V(points[n],points[n+1]),V(points[n+2],points[n+3]),c);
                }
                void Circle(float x,float y,float radius,Color c)
                {
                    for(int n=0;n<32;n++)
                    {
                        float a=n*Mathf.PI*2/32, b=(n+1)*Mathf.PI*2/32;
                        Triangle(vh,V(x,y),V(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),V(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),c);
                    }
                }
                // A light medallion keeps every glyph legible on both dark HUDs and the world.
                if (badge && Kind != "target") Circle(50,50,49,new Color32(255,252,243,245));
                switch(Kind)
                {
                    case "target":
                        for(int n=0;n<32;n++){float a=n*Mathf.PI*2/32,b=(n+1)*Mathf.PI*2/32;Segment(vh,V(50+40*Mathf.Cos(a),50+40*Mathf.Sin(a)),V(50+40*Mathf.Cos(b),50+40*Mathf.Sin(b)),size*.055f,new Color32(240,170,55,220));}break;
                    case "long-wave": Line(10,55,27,55,36,29,56,29,65,55,90,55); break;
                    case "basket": Poly(gold,15,35,85,35,74,85,26,85); Line(30,35,40,12,60,12,70,35);Line(36,45,40,75);Line(60,45,56,75); break;
                    case "hose": Line(12,86,12,65,20,55,44,55,56,42);Poly(gold,49,37,61,27,75,41,62,53);Line(73,26,85,14);Line(82,39,97,29);Line(64,18,70,3);break;
                    case "truck": Poly(gold,9,30,62,30,62,43,80,43,93,60,93,78,9,78);Circle(29,79,11,tint);Circle(76,79,11,tint);Line(16,19,65,19);Line(28,14,28,25);Line(43,14,43,25);break;
                    case "flame": Poly(new Color32(226,114,53,255),50,7,67,39,75,27,89,58,77,85,50,94,23,82,12,58,33,28,31,53);Poly(gold,50,44,67,73,53,87,36,74);break;
                    case "play": Poly(tint,30,14,84,50,30,86); break;
                    case "pause": Poly(tint,23,15,42,15,42,85,23,85); Poly(tint,58,15,77,15,77,85,58,85); break;
                    case "back": Line(85,50,15,50,43,20); Line(15,50,43,80); break;
                    case "next": Line(15,50,85,50,57,20); Line(85,50,57,80); break;
                    case "check": Line(15,52,40,78,88,22); break;
                    case "cross": Line(23,23,77,77); Line(23,77,77,23); break;
                    case "home": Poly(gold,15,85,15,36,50,9,85,36,85,85); Line(7,40,50,8,93,40); Line(42,85,42,58,60,58,60,85); break;
                    case "replay": case "rotate":
                        for(int n=0;n<24;n++){float a=(n*12+30)*Mathf.Deg2Rad,b=(n*12+42)*Mathf.Deg2Rad;Line(50+32*Mathf.Cos(a),50+32*Mathf.Sin(a),50+32*Mathf.Cos(b),50+32*Mathf.Sin(b));}
                        Poly(tint,64,10,88,15,72,37); break;
                    case "down": Line(50,12,50,85,23,58); Line(50,85,77,58); break;
                    case "diagonal": Line(18,18,80,80,80,44); Line(80,80,44,80); break;
                    case "horizontal": Line(10,50,90,50,70,30); Line(90,50,70,70); Line(10,50,30,30); Line(10,50,30,70); break;
                    case "sound": Poly(tint,12,37,29,37,52,17,52,83,29,63,12,63); Line(66,29,76,40,78,51,76,62,66,73); break;
                    case "muted": Poly(tint,12,37,29,37,52,17,52,83,29,63,12,63); Line(65,34,91,67); Line(91,34,65,67); break;
                    case "captions": Poly(gold,10,22,90,22,90,72,48,72,28,88,28,72,10,72); Line(24,40,76,40); Line(24,55,60,55); break;
                    case "eye": Poly(gold,7,50,25,30,50,22,75,30,93,50,75,70,50,78,25,70); Circle(50,50,13,tint); break;
                    case "shake": case "vibrate": Poly(gold,33,14,67,14,67,86,33,86); Line(19,23,11,40,21,60,13,79); Line(82,23,90,40,80,60,88,79); break;
                    case "star":
                        for(int n=0;n<10;n++){float a=(n*36-90)*Mathf.Deg2Rad,b=((n+1)*36-90)*Mathf.Deg2Rad;float r=n%2==0?43:20,s=n%2==0?20:43;Triangle(vh,V(50,50),V(50+Mathf.Cos(a)*r,50+Mathf.Sin(a)*r),V(50+Mathf.Cos(b)*s,50+Mathf.Sin(b)*s),(i+.5f)/repeats<=progress?gold:tint);} break;
                    case "timer": Circle(50,53,38,new Color32(220,232,220,255));
                        for(int n=0;n<32*Mathf.Clamp01(progress);n++){float a=(n*360f/32-90)*Mathf.Deg2Rad,b=((n+1)*360f/32-90)*Mathf.Deg2Rad;Triangle(vh,V(50,53),V(50+Mathf.Cos(a)*35,53+Mathf.Sin(a)*35),V(50+Mathf.Cos(b)*35,53+Mathf.Sin(b)*35),gold);} Line(50,17,50,5);Line(37,5,63,5);break;
                    case "dot": case "circle": Circle(50,50,31,tint); break;
                    case "square": Poly(tint,20,20,80,20,80,80,20,80); break;
                    case "triangle": Poly(tint,50,13,89,85,11,85); break;
                    case "route": Circle(20,76,9,tint); Circle(82,20,9,tint);Line(29,76,64,76,64,49,35,49,35,20,73,20);break;
                    case "team": Circle(27,24,12,tint);Circle(72,24,12,tint);Poly(gold,12,90,12,48,42,48,42,90);Poly(gold,57,90,57,48,87,48,87,90);Line(42,58,57,58);break;
                    case "waves": Line(8,58,18,34,28,58,38,34,48,58,58,34,68,58,78,34,88,58);break;
                    case "hold": Circle(51,51,44,new Color32(218,238,221,255)); goto default;
                    default: Poly(gold,32,57,32,20,40,13,48,20,48,47,57,39,65,42,70,49,81,49,86,60,76,86,49,91,19,64,23,53);Line(32,57,32,20,40,13,48,20,48,47);Line(18,26,8,15);Line(61,18,71,8);break;
                }
            }
        }
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        private static void Segment(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {Vector2 v=(b-a).normalized;Vector2 n=new Vector2(-v.y,v.x)*width*.5f;Triangle(vh,a-n,a+n,b+n,color);Triangle(vh,a-n,b+n,b-n,color);}
        private static void Quad(VertexHelper vh,Vector2 min,Vector2 max,Color color)
        {int n=vh.currentVertCount;vh.AddVert(min,color,new Vector2(0,0));vh.AddVert(new Vector2(min.x,max.y),color,new Vector2(0,1));vh.AddVert(max,color,new Vector2(1,1));vh.AddVert(new Vector2(max.x,min.y),color,new Vector2(1,0));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
