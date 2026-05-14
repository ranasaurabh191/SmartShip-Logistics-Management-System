# 🧪 SmartShip Frontend Testing Documentation

This document details the testing strategies, setup, and best practices used for the SmartShip Logistics Management System frontend.

## 🚀 Tech Stack
Our testing architecture mimics standard modern enterprise standards:
- **Test Runner**: [Vitest](https://vitest.dev/) (Natively integrates with Vite, API compatible with Jest/Jasmine)
- **DOM Simulation**: [JSDOM](https://github.com/jsdom/jsdom)
- **Component Testing**: [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)
- **Assertions**: BDD style `expect` (Jasmine/Jest-like matchers via `@testing-library/jest-dom`)

---

## 🛠️ Configuration Reference

### 1. Test Utility Structure
All tests live next to the components they are testing with the suffix `.test.tsx` or `.test.ts`.
```text
src/
├── components/
│   ├── Button.tsx
│   └── Button.test.tsx  # Specific unit/component test
└── store/
    ├── useAuthStore.ts
    └── useAuthStore.test.ts
```

### 2. Environment Configuration
Configuration lives in your `vite.config.ts`. Ensure the following block is present:
```typescript
import type { InlineConfig } from 'vitest';
import type { UserConfig } from 'vite';

interface VitestConfigExport extends UserConfig {
  test?: InlineConfig;
}

export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  },
} as VitestConfigExport);
```

---

## 📖 Key Testing Patterns

### A. Component Testing
Focus on visual presence and user behaviors instead of implementation details.

**Pattern Example:**
```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';

describe('ComponentName', () => {
  it('renders correct status text', () => {
    render(<Badge label="Active" />);
    expect(screen.getByText('Active')).toBeInTheDocument();
  });
});
```

### B. Service / Store Testing
When testing Zustand stores (like `useAuthStore`), reset persistent storage (sessionStorage) before each suite to keep tests clean and side-effect free.

**Pattern Example:**
```typescript
import { renderHook, act } from '@testing-library/react';

describe('Store Logic', () => {
  beforeEach(() => sessionStorage.clear());
  
  it('should update the count', () => {
    const { result } = renderHook(() => useCounterStore());
    act(() => result.current.increment());
    expect(result.current.count).toBe(1);
  });
});
```

### C. Route Guard Testing
Route Guards (like `DashboardLayout.tsx`) determine navigation. Test these by mocking `react-router-dom` (`useNavigate`) and the user roles to ensure the correct redirect conditions are triggered.

---

## 📡 CLI Commands

Once your scripts are registered in `package.json`, manage your test ecosystem using these commands:

| Command | Description |
|---------|-------------|
| `npm run test` | Execute all test suites once |
| `npm run test:watch` | Run tests in persistent, lightning-fast watch mode |
| `npm run test:coverage` | Analyze code logic coverage (branches/lines) |
| `npm run test:ui` | Boot up the interactive Vitest browser UI |

---

## ✅ Guiding Principles for Quality Specs

1. **Write Meaningful Descriptions**: Use the syntax `describe('ThingBeingTested')` -> `it('should behave like X when Y occurs')`.
2. **Query Accessibility First**: Prefer using `getByRole` or `getByText` to simulate the experience of a real screen-reader or user.
3. **Keep It Fast**: Mock expensive network I/O or complex integrations. Focus unit tests on pure application logic.
