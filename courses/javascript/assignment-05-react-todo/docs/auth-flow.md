# TaskFlow Authentication Flow

This document explains how login, JWT usage, and the silent
refresh-token rotation are implemented in TaskFlow.

## Components involved

| File | Role |
|------|------|
| `src/services/tokenStore.ts` | Module-scoped synchronous mirror of the active JWT and refresh token. Read by axios interceptors. |
| `src/services/apiClient.ts` | Axios instance for protected calls. Request interceptor adds `Bearer`, response interceptor performs refresh-on-401. |
| `src/services/AccountService.ts` | `Login` / `Register` requests against `/api/v1/Account/*`. |
| `src/reducers/authReducer.ts` | Pure reducer for `AuthState` transitions. |
| `src/context/AuthContext.tsx` | React provider that owns the reducer, rehydrates from `localStorage`, and bridges interceptor refreshes back into React state. |

## Why a separate `tokenStore`

The axios interceptors run outside React. They need synchronous access
to the current JWT and refresh token, and they need to update those
values when the backend rotates them. Reading directly from React state
would either require importing the React tree (impossible at module
load) or prop-drilling tokens into every service call.

`tokenStore` solves that by being a tiny module-scoped object. Its
content is mirrored into React state by `AuthContext` so the UI updates
naturally, but the interceptors never depend on the React tree.

## Sequence — login

```text
LoginPage.onSubmit
  └─ AuthContext.login(email, password)
       ├─ dispatch SET_LOADING true
       ├─ AccountService.login → POST /api/v1/Account/Login
       │      Response: { token, refreshToken, firstName, lastName }
       ├─ dispatch LOGIN_SUCCESS { token, refreshToken, email }
       │      └─ authReducer sets jwt + refreshToken + isAuthenticated
       └─ effect on [state.jwt, state.refreshToken, state.userEmail]
              ├─ localStorage.setItem(auth_jwt | auth_refreshToken | auth_userEmail)
              └─ tokenStore.setTokens(jwt, refreshToken)
```

`LoginPage` then calls `router.push("/todos")` and `ProtectedRoute`
admits the user.

## Sequence — rehydrate on cold start

```text
AuthProvider mount (useEffect [])
  ├─ read localStorage(jwt / refreshToken / userEmail)
  ├─ if all present:
  │     tokenStore.setTokens(jwt, refreshToken)
  │     dispatch AUTH_INIT { token, refreshToken, email }
  └─ else:
        dispatch SET_LOADING false
```

`ProtectedRoute` and `Home` both wait for `state.isLoading === false`
before deciding to redirect.

## Sequence — silent refresh on 401

```text
apiClient.get|post|put|delete
  ├─ request interceptor:
  │     headers.Authorization = `Bearer ${tokenStore.getToken()}`
  ├─ backend → 401 Unauthorized
  └─ response interceptor:
        if (status === 401 && !_retry):
          _retry = true
          POST /api/v1/Account/RefreshToken
            body: { jwt: <expired>, refreshToken: <current> }
          on success:
            tokenStore.setTokens(newToken, newRefresh)
            onTokenRefreshed?.(newToken, newRefresh)   ← AuthContext callback
            retry original request with new Bearer
          on failure:
            tokenStore.clearTokens()
            window.location.href = "/login"
```

`AuthContext` subscribes to the refresh callback via
`setOnTokenRefreshed`. When it fires, the context dispatches
`TOKEN_REFRESHED`, the reducer updates `jwt` and `refreshToken`, and the
mirror effect writes the new values into `localStorage`. The user sees
nothing — the original request completes with fresh tokens.

## Sequence — logout

```text
NavBar logout button
  └─ AuthContext.logout()
       ├─ dispatch LOGOUT
       │     └─ authReducer resets to initialAuthState (isLoading: false)
       ├─ tokenStore.clearTokens()
       └─ mirror effect clears localStorage(auth_jwt | auth_refreshToken | auth_userEmail)
```

## Reducer summary

```text
state := { jwt, refreshToken, userEmail, isAuthenticated, isLoading, error }

AUTH_INIT      / LOGIN_SUCCESS   → isAuthenticated: true,  isLoading: false
LOGOUT                           → initialAuthState ∪ { isLoading: false }
TOKEN_REFRESHED                  → jwt, refreshToken updated in place
AUTH_ERROR                       → error: msg, isLoading: false
SET_LOADING                      → isLoading: bool
```

## Properties that hold

- A 401 from any protected endpoint either refreshes silently or sends
  the user to `/login` — never bubbles up as a visible error.
- The refresh attempt is gated by `_retry` to avoid infinite loops on a
  permanently invalid refresh token.
- React state never lags the active Bearer used by axios:
  `tokenStore` is updated before the dispatch and `localStorage` is
  updated in the mirror effect.
- All tokens are cleared in three places on logout (state, store,
  storage) so a stale token cannot survive a page reload.

## No prop drilling — where state is consumed

| Consumer | Hook |
|----------|------|
| `NavBar` (email, logout button) | `useAuth()` |
| `ProtectedRoute` (gate) | `useAuth()` |
| `Home`, `LoginPage`, `RegisterPage` (redirect logic) | `useAuth()` |
| `TodosPage`, `TodoEditorPage`, `CategoriesPage`, `PrioritiesPage` | `useTodo()` (+ `useAuth()` for the gated effects) |
| `TodoRow` (toggle/delete callbacks) | `useTodo()` |

No component receives auth tokens, user identity, or the dispatch
function as props.
