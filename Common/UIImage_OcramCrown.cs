using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.UI;

namespace Consolaria.Common;

public class UIImage_OcramCrown : UIElement {
    private Asset<Texture2D> _texture;
    public float ImageScale = 1f;
    public float Rotation;
    public bool ScaleToFit;
    public bool AllowResizingDimensions = true;
    public Color Color = Color.White;
    public Vector2 NormalizedOrigin = Vector2.Zero;
    public bool RemoveFloatingPointsFromDrawPosition;
    private Texture2D _nonReloadingTexture;

    public UIImage_OcramCrown(Asset<Texture2D> texture) {
        SetImage(texture);
    }

    public UIImage_OcramCrown(Texture2D nonReloadingTexture) {
        SetImage(nonReloadingTexture);
    }

    public void SetImage(Asset<Texture2D> texture) {
        _texture = texture;
        _nonReloadingTexture = null;
        if (AllowResizingDimensions) {
            Width.Set(_texture.Width(), 0f);
            Height.Set(_texture.Height(), 0f);
        }
    }

    public void SetImage(Texture2D nonReloadingTexture) {
        _texture = null;
        _nonReloadingTexture = nonReloadingTexture;
        if (AllowResizingDimensions) {
            Width.Set(_nonReloadingTexture.Width, 0f);
            Height.Set(_nonReloadingTexture.Height, 0f);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        CalculatedStyle dimensions = GetDimensions();
        Texture2D texture2D = null;
        if (_texture != null)
            texture2D = _texture.Value;

        if (_nonReloadingTexture != null)
            texture2D = _nonReloadingTexture;

        ShaderLoader.DistortShader.SetDefault(texture2D.Width * 2, texture2D.Height * 2);
        ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
            if (ScaleToFit) {
                spriteBatch.Draw(texture2D, dimensions.ToRectangle(), Color);
                return;
            }

            Vector2 vector = texture2D.Size();
            Vector2 vector2 = dimensions.Position() + vector * (1f - ImageScale) / 2f + vector * NormalizedOrigin;
            if (RemoveFloatingPointsFromDrawPosition)
                vector2 = vector2.Floor();

            spriteBatch.Draw(texture2D, vector2, null, Color.MultiplyRGBA(EternalHorror.MainPurpleColor).MultiplyAlpha(1f), Rotation, vector * NormalizedOrigin, ImageScale, SpriteEffects.None, 0f);
        });
    }
}
