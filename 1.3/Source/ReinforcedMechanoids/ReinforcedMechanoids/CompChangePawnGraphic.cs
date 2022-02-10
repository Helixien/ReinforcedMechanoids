using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ReinforcedMechanoids
{
    public class PawnGraphicByMissingParts
    {
        public BodyPartTagDef missingPart;
        public string texPath;
    }
    public class CompProperties_ChangeGraphic : CompProperties
    {
        public List<PawnGraphicByMissingParts> pawnGraphicsByMissingParts;
        public CompProperties_ChangeGraphic()
        {
            this.compClass = typeof(CompChangePawnGraphic);
        }
    }

    [StaticConstructorOnStartup]
    public class CompChangePawnGraphic : ThingComp
    {
        public string currentTexPath;
        public PawnRenderer PawnRenderer => (this.parent as Pawn).Drawer.renderer;
        public CompProperties_ChangeGraphic Props => base.props as CompProperties_ChangeGraphic;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.TryChangeGraphic(true);
        }

        public void TryChangeGraphic(bool changeAnyWay)
        {
            var pawn = this.parent as Pawn;
            foreach (var pawnGraphic in Props.pawnGraphicsByMissingParts)
            {
                var bodyPart = Core.GetNonMissingBodyPart(pawn, pawnGraphic.missingPart);
                if (bodyPart != null)
                {
                    ChangeGraphic(pawn, pawn.ageTracker.CurKindLifeStage.bodyGraphicData.texPath);
                    return;
                }
                else if (changeAnyWay || pawnGraphic.texPath != this.currentTexPath)
                {
                    ChangeGraphic(pawn, pawnGraphic.texPath);
                    return;
                }
            }
        }

        private void ChangeGraphic(Pawn pawn, string texPath)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                Graphic_Multi nakedGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(texPath, ShaderDatabase.Cutout, pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize, Color.white);
                PawnRenderer.graphics.ResolveAllGraphics();
                PawnRenderer.graphics.nakedGraphic = nakedGraphic;
                currentTexPath = texPath;
            });
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentTexPath, "currentTexPath");
        }
    }
}
