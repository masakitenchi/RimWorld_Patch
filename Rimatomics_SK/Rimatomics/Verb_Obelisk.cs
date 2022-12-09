using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimatomics
{
	[StaticConstructorOnStartup]
	public class Verb_Obelisk : Verb_RimatomicsVerb
	{
		public override void WarmupComplete()
		{
			base.WarmupComplete();
			Find.BattleLog.Add(new BattleLogEntry_RangedFire(caster, (!currentTarget.HasThing) ? null : currentTarget.Thing, base.EquipmentSource?.def, null, burst: false));
		}

		public override bool TryCastShot()
		{
			Building_EnergyWeapon getWep = base.GetWep;
			if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
			{
				return false;
			}
			ShootLine resultingLine;
			bool flag = !getWep.AttackVerb.verbProps.requireLineOfSight || TryFindShootLineFromTo(caster.Position, currentTarget, out resultingLine);
			if (verbProps.stopBurstWithoutLos && !flag)
			{
				return false;
			}
			bool flag2 = Rand.Chance(0.042f);
			float num = getWep.Damage;
			if (flag2)
			{
				num *= 4.2f;
			}
			DamageWorker.DamageResult damageResult = currentTarget.Thing.TakeDamage(new DamageInfo(DubDef.ArcDischarge, num, 5f, -1f, caster, null, getWep.def.building.turretGunDef));
			float num2 = damageResult.totalDamageDealt;
			if (getWep.UG.HasUpgrade(DubDef.BeamSplitter))
			{
				damageResult = currentTarget.Thing.TakeDamage(new DamageInfo(DubDef.ArcDischarge, num, 5f, -1f, caster, null, getWep.def.building.turretGunDef));
				num2 += damageResult.totalDamageDealt;
			}
			getWep.DamageDealt = num2;
			Vector3 loc = currentTarget.Cell.ToVector3Shifted();
			for (int i = 0; i < 3; i++)
			{
				FleckMaker.ThrowSmoke(loc, getWep.Map, 1.5f);
				FleckMaker.ThrowMicroSparks(loc, getWep.Map);
			}
			if (flag2)
			{
				FleckMaker.ThrowLightningGlow(loc, getWep.Map, 1f);
			}
			Pawn pawn = currentTarget.Thing as Pawn;
			if ((pawn?.Dead ?? false) && flag2)
			{
				DubDef.Sizzle.PlayOneShot(SoundInfo.InMap(new TargetInfo(currentTarget.Thing)));
				CompRottable compRottable = pawn.Corpse.TryGetComp<CompRottable>();
				if (compRottable != null)
				{
					compRottable.RotProgress = 1E+10f;
				}
			}
			getWep.DissipateCharge(getWep.PulseSize);
			getWep.GatherData("PPCWeapon", 5f);
			getWep.GatherData("PPCObelisk", 5f);
			getWep.PrototypeBang(getWep.GunProps.EnergyWep.PrototypeFailureChance);
			Mote_Beam obj = (Mote_Beam)ThingMaker.MakeThing(DubDef.Mote_Beam);
			obj.SetupMoteBeam(GraphicsCache.obeliskBeam, getWep.TipOffset, currentTarget.Thing.DrawPos);
			obj.Attach(getWep);
			GenSpawn.Spawn(obj, getWep.Position, getWep.Map);
			return true;
		}
	}
}