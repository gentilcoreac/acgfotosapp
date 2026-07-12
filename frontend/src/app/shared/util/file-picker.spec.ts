import { pickFile } from './file-picker';

describe('pickFile', () => {
  it('resuelve con el archivo elegido', async () => {
    const file = new File(['x'], 'DimTest.xlsx');
    vi.spyOn(HTMLInputElement.prototype, 'click').mockImplementation(function (
      this: HTMLInputElement,
    ) {
      Object.defineProperty(this, 'files', { value: [file] });
      this.onchange?.(new Event('change'));
    });

    await expect(pickFile('.xlsx')).resolves.toBe(file);
  });

  it('resuelve null si el usuario cancela', async () => {
    vi.spyOn(HTMLInputElement.prototype, 'click').mockImplementation(function (
      this: HTMLInputElement,
    ) {
      this.oncancel?.(new Event('cancel'));
    });

    await expect(pickFile('.xlsx')).resolves.toBeNull();
  });
});
