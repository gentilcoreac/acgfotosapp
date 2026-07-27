import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth';
import { ApiError } from '../../../core/models';
import { OlvidePasswordComponent } from './olvide-password.component';

describe('OlvidePasswordComponent', () => {
  let fixture: ComponentFixture<OlvidePasswordComponent>;
  let auth: MockedObject<AuthService>;

  async function setup(): Promise<void> {
    auth = {
      olvidePassword: vi.fn().mockName('AuthService.olvidePassword'),
    } as unknown as MockedObject<AuthService>;

    await TestBed.configureTestingModule({
      imports: [OlvidePasswordComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OlvidePasswordComponent);
    fixture.detectChanges();
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const component = (): any => fixture.componentInstance;

  const setEmailOrUsername = (value: string): void => {
    const input = fixture.nativeElement.querySelector('tbi-text-field input') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('no llama a olvidePassword con el form inválido', async () => {
    await setup();
    submit();
    expect(auth.olvidePassword).not.toHaveBeenCalled();
  });

  it('envía el usuario/email y muestra el aviso de éxito', async () => {
    await setup();
    auth.olvidePassword.mockReturnValue(of(undefined));
    setEmailOrUsername('root');
    submit();
    expect(auth.olvidePassword).toHaveBeenCalledWith({ emailOrUsername: 'root' });
    expect(component().enviado()).toBe(true);
  });

  it('ante error muestra los mensajes del ApiError', async () => {
    await setup();
    const apiError: ApiError = {
      status: 400,
      message: 'ErrorEmailNotCorrespondToRegisteredUser',
      errors: [],
    };
    auth.olvidePassword.mockReturnValue(throwError(() => apiError));
    setEmailOrUsername('no-existe');
    submit();
    expect(component().errors()).toEqual(['ErrorEmailNotCorrespondToRegisteredUser']);
    expect(component().loading()).toBe(false);
  });
});
