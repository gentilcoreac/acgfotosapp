import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  let fixture: ComponentFixture<NotFoundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();
  });

  it('muestra el 404 y un link al home', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="not-found-page"]')).toBeTruthy();
    expect(el.textContent).toContain('404');
    const home = el.querySelector('[data-testid="error-go-home"]') as HTMLAnchorElement;
    expect(home).toBeTruthy();
    expect(home.getAttribute('href')).toBe('/');
  });
});
