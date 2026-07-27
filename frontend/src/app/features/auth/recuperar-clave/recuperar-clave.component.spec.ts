import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { RecuperarClaveComponent } from './recuperar-clave.component';

describe('RecuperarClaveComponent', () => {
  let fixture: ComponentFixture<RecuperarClaveComponent>;
  let auth: MockedObject<AuthService>;
  let router: MockedObject<Router>;
  let notify: MockedObject<NotificationService>;

  /** `params` simula los query params del link del mail. */
  async function setup(params: Record<string, string>): Promise<void> {
    auth = {
      resetearPassword: vi.fn().mockName('AuthService.resetearPassword'),
    } as unknown as MockedObject<AuthService>;
    router = {
      navigateByUrl: vi.fn().mockName('Router.navigateByUrl'),
    } as unknown as MockedObject<Router>;
    notify = {
      error: vi.fn().mockName('NotificationService.error'),
      success: vi.fn().mockName('NotificationService.success'),
    } as unknown as MockedObject<NotificationService>;

    await TestBed.configureTestingModule({
      imports: [RecuperarClaveComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: NotificationService, useValue: notify },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(params) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RecuperarClaveComponent);
    fixture.detectChanges();
  }

  const validParams = { userId: '5', code: 'tok-123', userName: 'root', cliente: '1' };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const component = (): any => fixture.componentInstance;

  const setPassword = (pwd: string, confirm: string): void => {
    const inputs = fixture.nativeElement.querySelectorAll(
      'tbi-text-field input',
    ) as NodeListOf<HTMLInputElement>;
    inputs[0].value = pwd;
    inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = confirm;
    inputs[1].dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('muestra el aviso de enlace inválido si faltan userName/code', async () => {
    await setup({});
    expect(component().linkInvalido).toBe(true);
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });

  it('no resetea si las contraseñas no coinciden', async () => {
    await setup(validParams);
    setPassword('Techbi2026!@', 'Otraclave2026!@');
    submit();
    expect(auth.resetearPassword).not.toHaveBeenCalled();
  });

  it('resetea enviando userName/code + password y navega a login', async () => {
    await setup(validParams);
    auth.resetearPassword.mockReturnValue(of('Contraseña actualizada'));
    setPassword('Techbi2026!@', 'Techbi2026!@');
    submit();
    expect(auth.resetearPassword).toHaveBeenCalled();
    const payload = vi.mocked(auth.resetearPassword).mock.lastCall![0];
    expect(payload.emailOrUsername).toBe('root');
    expect(payload.code).toBe('tok-123');
    expect(payload.password).toBe('Techbi2026!@');
    expect(payload.confirmPassword).toBe('Techbi2026!@');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
