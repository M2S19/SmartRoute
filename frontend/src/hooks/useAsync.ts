import { useCallback, useState } from 'react';
import { toErrorCode, toErrorMessage } from '../types';
import type { ApiErrorCode } from '../types';

export type AsyncState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: string; code: ApiErrorCode };

/**
 * États d'un appel de service : idle → loading → success | error.
 * `run` renvoie la donnée, ou undefined si l'appel a échoué.
 */
export function useAsync<TArgs extends unknown[], TResult>(action: (...args: TArgs) => Promise<TResult>) {
  const [state, setState] = useState<AsyncState<TResult>>({ status: 'idle' });

  const run = useCallback(
    async (...args: TArgs): Promise<TResult | undefined> => {
      setState({ status: 'loading' });
      try {
        const data = await action(...args);
        setState({ status: 'success', data });
        return data;
      } catch (error) {
        setState({ status: 'error', error: toErrorMessage(error), code: toErrorCode(error) });
        return undefined;
      }
    },
    [action],
  );

  const reset = useCallback(() => setState({ status: 'idle' }), []);

  return { state, run, reset, isLoading: state.status === 'loading' };
}
