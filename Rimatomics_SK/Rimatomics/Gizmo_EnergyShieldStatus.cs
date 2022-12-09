using UnityEngine;
using Verse;

namespace Rimatomics
{
	[StaticConstructorOnStartup]
	public class Gizmo_EnergyShieldStatus : Gizmo
	{
		public CompRimatomicsShield shield;

		private static readonly Texture2D FullShieldBarTex;

		private static readonly Texture2D EmptyShieldBarTex;

		public Gizmo_EnergyShieldStatus()
		{
			base.order = -100f;
		}

		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms p)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect rect2 = rect.ContractedBy(6f);
			Widgets.DrawWindowBackground(rect);
			Rect rect3 = rect2;
			rect3.height = rect.height / 2f;
			Text.Font = GameFont.Tiny;
			Widgets.Label(rect3, shield.parent.LabelCap);
			Rect rect4 = rect2;
			rect4.yMin = rect2.y + rect2.height / 2f;
			float fillPercent = shield.energy / Mathf.Max(1f, shield.EnergyMax);
			Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect4, (shield.energy * 100f).ToString("F0") + " / " + (shield.EnergyMax * 100f).ToString("F0"));
			Text.Anchor = TextAnchor.UpperLeft;
			return new GizmoResult(GizmoState.Clear);
		}

		static Gizmo_EnergyShieldStatus()
		{
			FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
			EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
		}
	}
}
