---
name: conventional-commit
description: Genera mensajes de commit siguiendo la convención de Conventional Commits. Se usa al hacer un commit o cuando el usuario pide un mensaje de commit.
---

# Conventional Commit

1. Antes de generar el mensaje de commit, hacé un "git diff --staged" para conocer los cambios a commitear.

2. A partir de esos cambios, tenés que armar un comentario con el siguiente formato:

tipo(scope): descripción en imperativo

donde:
El tipo dice qué clase de cambio es: feat (feature nueva), fix (arreglo), docs (documentación), refactor, test, chore (mantenimiento). Todo cambio debe ser encasillado en alguna de estas opciones.
El scope (opcional) dice qué parte del proyecto toca. Si no es posible determinar un scope certero, se deja vacío
La descripción del cambio: en imperativo, minúscula, sin punto final, corta (≤ 72 caracteres).

Ejemplos:
feat(auth): agregar validación de email en el registro
fix(tickets): rechazar tickets con asunto vacío
docs(prd): aclarar criterio de control de acceso

3. Si rompe compatibilidad, agregá `BREAKING CHANGE:` en el cuerpo.