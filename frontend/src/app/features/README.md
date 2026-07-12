# features/

Una carpeta por feature de negocio, **lazy-loaded**. Cada feature es autocontenida:

```
features/<feature>/
├── data/        # servicios HTTP + DTOs + mappers
├── domain/      # modelos + store de la feature (signals)
├── ui/          # componentes (list, edit, etc.)
└── <feature>.routes.ts
```

Features de plataforma heredadas del shell: roles, permisos, usuarios, tenants, parámetros, etc.
El vertical de negocio de AcgFotos (`fotos`: eventos, álbumes, galería, pedidos) se agrega acá como
features nuevas. Cada feature se registra con `loadChildren`/`loadComponent` en el routing.
