using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Align360
{
    // Uses the project's authored Figma exports; no flattened screen images.
    public sealed class Align360Theme
    {
        public readonly Color Gold = new Color(1f,.77f,.25f);
        public readonly Color Navy = new Color(.025f,.045f,.085f,.97f);
        public readonly Color Cream = new Color(.96f,.9f,.73f);
        public readonly Color Muted = new Color(.65f,.71f,.73f);
        public readonly Color PaperInk = new Color(.19f,.14f,.08f);
        private readonly List<Sprite> sprites = new List<Sprite>();
        private Sprite panel,button;
        private Sprite Load(string name,Vector4 border)
        {
            var texture=Resources.Load<Texture2D>("Align360/"+name);
            if(texture==null)return null;
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            sprites.Add(sprite);return sprite;
        }
        public void Card(Image image)
        {
            if(panel==null)panel=Load("panel",new Vector4(24,24,24,24));
            image.sprite=null;image.color=Navy;
            var edge=new GameObject("Gold frame",typeof(RectTransform),typeof(Image));edge.transform.SetParent(image.transform,false);
            var rect=edge.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var frame=edge.GetComponent<Image>();frame.sprite=panel;frame.type=Image.Type.Sliced;frame.color=Color.white;frame.raycastTarget=false;
        }
        public void Button(Image image,bool primary)
        {
            if(primary) { if(button==null)button=Load("button",new Vector4(38,38,38,38));image.sprite=button;image.type=Image.Type.Sliced;image.color=Color.white; }
            else Card(image);
        }
        public void Dispose() { foreach(var sprite in sprites) if(sprite!=null)Object.Destroy(sprite); }
    }
}
