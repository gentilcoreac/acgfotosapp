import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { ProfileService } from '../data/profile.service';
import { Perfil } from '../domain/profile.model';
import { ProfileComponent } from './profile.component';

const perfil: Perfil = {
  id: 1,
  userName: 'jdoe',
  nombre: 'Juan',
  apellido: 'Doe',
  email: 'jdoe@tenant.com',
  telefono: 3411234567,
  administrador: true,
  emailConfirmed: true,
  claveVigente: true,
  profilePicture: null,
};

describe('ProfileComponent', () => {
  let fixture: ComponentFixture<ProfileComponent>;
  let service: MockedObject<ProfileService>;
  let notify: MockedObject<NotificationService>;

  function setup(): void {
    fixture = TestBed.createComponent(ProfileComponent);
    fixture.detectChanges();
  }

  beforeEach(() => {
    service = {
      getPerfil: vi.fn().mockName('ProfileService.getPerfil'),
      updateProfile: vi.fn().mockName('ProfileService.updateProfile'),
      changePassword: vi.fn().mockName('ProfileService.changePassword'),
    } as unknown as MockedObject<ProfileService>;
    notify = {
      success: vi.fn().mockName('NotificationService.success'),
      error: vi.fn().mockName('NotificationService.error'),
    } as unknown as MockedObject<NotificationService>;
    service.getPerfil.mockReturnValue(of(perfil));
    service.updateProfile.mockReturnValue(of({}));
    service.changePassword.mockReturnValue(of({}));

    TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        { provide: ProfileService, useValue: service },
        { provide: NotificationService, useValue: notify },
      ],
    });
  });

  it('carga el perfil y muestra userName/email + chip admin', () => {
    setup();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="profile-user-name"]')?.textContent).toContain('jdoe');
    expect(el.querySelector('[data-testid="profile-email"]')?.textContent).toContain(
      'jdoe@tenant.com',
    );
    expect(el.querySelector('[data-testid="profile-admin-chip"]')).toBeTruthy();
    expect(el.querySelector('[data-testid="profile-email-verified"]')).toBeTruthy();
  });

  it('save() manda los datos editados conservando los read-only y avisa éxito', () => {
    setup();
    fixture.componentInstance['datosForm'].setValue({
      nombre: 'Juana',
      apellido: 'Doe',
      telefono: '3419999999',
    });
    fixture.componentInstance['save']();
    expect(service.updateProfile).toHaveBeenCalled();
    const enviado = vi.mocked(service.updateProfile).mock.lastCall![0];
    expect(enviado.nombre).toBe('Juana');
    expect(enviado.telefono).toBe(3419999999);
    expect(enviado.userName).toBe('jdoe'); // read-only conservado
    expect(enviado.email).toBe('jdoe@tenant.com');
    expect(notify.success).toHaveBeenCalled();
  });

  it('changeMyPassword() no llama a la API si las contraseñas no coinciden', () => {
    setup();
    fixture.componentInstance['passwordForm'].setValue({
      currentPassword: 'actual1',
      newPassword: 'nueva123',
      newConfirmPassword: 'otra123',
    });
    fixture.componentInstance['changeMyPassword']();
    expect(service.changePassword).not.toHaveBeenCalled();
  });

  it('changeMyPassword() llama a la API cuando es válido y avisa éxito', () => {
    setup();
    fixture.componentInstance['passwordForm'].setValue({
      currentPassword: 'actual1',
      newPassword: 'nueva123',
      newConfirmPassword: 'nueva123',
    });
    fixture.componentInstance['changeMyPassword']();
    expect(service.changePassword).toHaveBeenCalled();
    expect(notify.success).toHaveBeenCalled();
  });
});
