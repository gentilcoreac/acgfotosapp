// Conventional Commits: prefijo de tipo + asunto en español, en minúscula.
// Tipos válidos: feat, fix, refactor, docs, test, build, ci, chore, perf, style, revert.
// Ejemplo: "feat: se agrega la capa de autenticación del cliente".
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'header-max-length': [2, 'always', 100],
    'body-leading-blank': [2, 'always'],
    'footer-leading-blank': [2, 'always'],
  },
};
