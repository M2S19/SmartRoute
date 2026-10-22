import { useEffect, useState } from 'react';
import { Platform, StyleSheet, Text, View } from 'react-native';
import { Button, Card, Icon, Notice, ScreenContainer, TextField } from '../../components/common';
import { MOCKS } from '../../config/api';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import { useAsync } from '../../hooks/useAsync';
import { useAuth } from '../../hooks/useAuth';
import { healthService } from '../../services';
import { isNonEmpty, isValidEmail } from '../../utils/validation';

// Indicateur de connexion à l'API : en développement, et seulement si le login passe par l'API.
const SHOW_API_STATUS = __DEV__ && !MOCKS.auth;

interface FieldErrors {
  email?: string;
  password?: string;
}

export function LoginScreen() {
  const { login, sessionMessage } = useAuth();
  const { state, run, isLoading } = useAsync(login);
  const health = useAsync(healthService.check);
  const runHealthCheck = health.run;
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  useEffect(() => {
    if (SHOW_API_STATUS) void runHealthCheck();
  }, [runHealthCheck]);

  const handleSubmit = () => {
    if (isLoading) return;
    const errors: FieldErrors = {};
    if (!isValidEmail(email)) errors.email = 'Saisissez une adresse email valide.';
    if (!isNonEmpty(password)) errors.password = 'Saisissez votre mot de passe.';
    setFieldErrors(errors);
    if (errors.email || errors.password) return;

    // En cas de succès, RootNavigator bascule automatiquement vers l'espace du rôle.
    void run({ email: email.trim(), password });
  };

  return (
    <ScreenContainer contentStyle={styles.content}>
      <View style={styles.inner}>
        <View style={styles.brand}>
          <View style={styles.logo}>
            <Icon name="alt-route" size={sizes.iconLg} color={colors.primary} />
          </View>
          <Text style={styles.brandName}>SmartRoute</Text>
        </View>

        <Card style={styles.card}>
          <View style={styles.cardHeader}>
            <Text style={styles.cardTitle}>Connexion</Text>
            <Text style={styles.cardSubtitle}>Veuillez vous identifier pour accéder à votre espace.</Text>
          </View>

          <View style={styles.form}>
            <TextField
              label="Email"
              value={email}
              onChangeText={setEmail}
              placeholder="nom@exemple.fr"
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
              autoComplete="email"
              textContentType="emailAddress"
              returnKeyType="next"
              editable={!isLoading}
              error={fieldErrors.email}
            />
            <TextField
              label="Mot de passe"
              value={password}
              onChangeText={setPassword}
              placeholder="••••••••"
              secure
              autoCapitalize="none"
              autoComplete="password"
              textContentType="password"
              returnKeyType="go"
              onSubmitEditing={handleSubmit}
              editable={!isLoading}
              error={fieldErrors.password}
            />

            {state.status === 'error' ? <Notice tone="error" message={state.error} /> : null}
            {state.status !== 'error' && sessionMessage ? <Notice tone="info" message={sessionMessage} /> : null}

            <Button label="Se connecter" onPress={handleSubmit} loading={isLoading} style={styles.submit} />
          </View>
        </Card>

        {SHOW_API_STATUS ? (
          <Text style={styles.apiStatus} accessibilityLiveRegion="polite">
            {health.state.status === 'success'
              ? `API joignable : ${healthService.baseUrl}`
              : health.state.status === 'error'
                ? `API injoignable (${healthService.baseUrl}) : ${health.state.error}`
                : `Vérification de l'API : ${healthService.baseUrl}…`}
          </Text>
        ) : null}
      </View>
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  content: {
    justifyContent: 'center',
    paddingVertical: spacing.xl,
  },
  inner: {
    flex: 1,
    justifyContent: 'center',
    width: '100%',
    maxWidth: 384, // max-w-sm
    alignSelf: 'center',
  },
  brand: {
    alignItems: 'center',
    marginBottom: spacing.xl,
  },
  logo: {
    width: sizes.logo,
    height: sizes.logo,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surfaceContainerHigh,
    marginBottom: spacing.sm,
  },
  brandName: {
    ...typography.headlineLgMobile,
    color: colors.onSurface,
  },
  card: {
    padding: spacing.margin,
    borderColor: 'transparent',
    ...Platform.select({
      ios: { shadowColor: '#000', shadowOpacity: 0.05, shadowRadius: 2, shadowOffset: { width: 0, height: 1 } },
      android: { elevation: 1 },
      default: {},
    }),
  },
  cardHeader: {
    marginBottom: spacing.lg,
  },
  cardTitle: {
    ...typography.headlineSm,
    color: colors.onSurface,
  },
  cardSubtitle: {
    ...typography.bodyMd,
    color: colors.onSurfaceVariant,
    marginTop: spacing.xs,
  },
  form: {
    gap: spacing.lg,
  },
  submit: {
    marginTop: spacing.sm,
  },
  apiStatus: {
    ...typography.bodySm,
    color: colors.secondary,
    textAlign: 'center',
    marginTop: spacing.md,
  },
});
