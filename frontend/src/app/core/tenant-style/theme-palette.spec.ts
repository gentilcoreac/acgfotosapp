import { buildTenantSysVars } from './theme-palette';

describe('buildTenantSysVars', () => {
  it('genera variables --mat-sys-* de color como light-dark(claro, oscuro)', () => {
    const vars = buildTenantSysVars('#C0392B', '#E74C3C');

    expect(Object.keys(vars).length).toBeGreaterThan(40);
    expect(Object.keys(vars).every((k) => k.startsWith('--mat-sys-'))).toBe(true);
    expect(vars['--mat-sys-primary']).toMatch(/^light-dark\(#[0-9a-f]{6}, #[0-9a-f]{6}\)$/i);
    expect(vars['--mat-sys-surface']).toBeDefined();
    expect(vars['--mat-sys-on-primary-container']).toBeDefined();
    expect(vars['--mat-sys-surface-container-high']).toBeDefined();
  });

  it('semillas distintas producen un primary distinto', () => {
    const red = buildTenantSysVars('#C0392B', '#E74C3C')['--mat-sys-primary'];
    const blue = buildTenantSysVars('#1A5FB4', '#3584E4')['--mat-sys-primary'];
    expect(red).not.toBe(blue);
  });
});
