import { MaterialIcons } from '@expo/vector-icons';

/** Équivalents MaterialIcons des Material Symbols utilisés par Stitch. */
export type IconName = keyof typeof MaterialIcons.glyphMap;

export { MaterialIcons as Icon };
