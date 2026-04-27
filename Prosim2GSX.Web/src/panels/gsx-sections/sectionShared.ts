import { GsxSettingsDto, ServiceConfigDto } from "../../types";

// Props every GSX sub-section receives. Sections destructure only the
// helpers they need; unused destructured props don't trigger
// noUnusedParameters because the linter only catches unused declared
// parameters and locals, not unused fields of a destructured object.
export interface GsxSectionProps {
  draft: GsxSettingsDto;
  update: <K extends keyof GsxSettingsDto>(key: K, value: GsxSettingsDto[K]) => void;

  updateService: (idx: number, partial: Partial<ServiceConfigDto>) => void;
  moveService: (idx: number, dir: -1 | 1) => void;
  removeService: (idx: number) => void;
  addService: () => void;

  updateListItem: (field: "operatorPreferences" | "companyHubs", idx: number, value: string) => void;
  addListItem: (field: "operatorPreferences" | "companyHubs") => void;
  removeListItem: (field: "operatorPreferences" | "companyHubs", idx: number) => void;
}
