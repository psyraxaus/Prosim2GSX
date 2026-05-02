import { Section } from "../../components/forms/Section";
import { BoolField, NumberField } from "../../components/forms/Field";
import { GsxSectionProps } from "./sectionShared";

export function SkipQuestionsSection({ draft, update }: GsxSectionProps) {
  return (
    <Section title="Skip Questions / Pop-ups">
      <BoolField label="Skip Walkaround" value={draft.skipWalkAround}
        onChange={(v) => update("skipWalkAround", v)} />
      <BoolField label="Skip Crew Question" value={draft.skipCrewQuestion}
        onChange={(v) => update("skipCrewQuestion", v)} />
      <BoolField label="Skip Follow Me" value={draft.skipFollowMe}
        onChange={(v) => update("skipFollowMe", v)} />
      <BoolField label="Keep Direction Menu Open" value={draft.keepDirectionMenuOpen}
        onChange={(v) => update("keepDirectionMenuOpen", v)} />
      <BoolField label="Answer Cabin Call (Ground)" value={draft.answerCabinCallGround}
        onChange={(v) => update("answerCabinCallGround", v)} />
      <NumberField label="Cabin Call Delay (Ground, ms)" value={draft.delayCabinCallGround}
        onChange={(v) => update("delayCabinCallGround", v)} />
      <BoolField label="Answer Cabin Call (Air)" value={draft.answerCabinCallAir}
        onChange={(v) => update("answerCabinCallAir", v)} />
      <NumberField label="Cabin Call Delay (Air, ms)" value={draft.delayCabinCallAir}
        onChange={(v) => update("delayCabinCallAir", v)} />
    </Section>
  );
}
