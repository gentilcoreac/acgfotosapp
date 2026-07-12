/**
 * Modo de color de la app. Dos estados persistibles, alineados al toggle del toolbar; la
 * preferencia del SO (`prefers-color-scheme`) se usa solo como semilla inicial, no se persiste
 * como un modo "system".
 */
export type ThemeMode = 'light' | 'dark';
