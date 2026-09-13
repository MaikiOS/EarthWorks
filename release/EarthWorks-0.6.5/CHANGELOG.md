# Changelog

## 0.6.5

- Removed the obsolete half-cell paint offset; writes use stored native terrain-grid coordinates.
- Preserved existing paint-mask alpha when applying dirt or paving.
- Added deterministic 65×65 paint-grid corner, border, and invalid-index tests.
- Replaced route bounding-circle grass clearing with sampled corridor refreshes.
- Expanded English/Russian roadmap and contribution guidance.

## 0.6.5 — русский

- Удалён устаревший paint offset на полклетки; запись использует штатные grid-координаты.
- При dirt/paving сохраняется существующий alpha paint-mask.
- Добавлены детерминированные тесты углов, border и неверных индексов paint-grid 65×65.
- Очистка травы по bounding-circle заменена обновлением вдоль sampled-коридора.
- Расширены двуязычные roadmap и руководство для contributors.
