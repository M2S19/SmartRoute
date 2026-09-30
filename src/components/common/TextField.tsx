import { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, TextInputProps, View } from 'react-native';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import { Icon } from './Icon';

interface TextFieldProps extends Omit<TextInputProps, 'style' | 'secureTextEntry'> {
  label?: string;
  /** Champ mot de passe avec bouton afficher / masquer. */
  secure?: boolean;
  error?: string;
}

export function TextField({ label, secure = false, error, editable = true, onFocus, onBlur, ...inputProps }: TextFieldProps) {
  const [hidden, setHidden] = useState(secure);
  const [focused, setFocused] = useState(false);

  return (
    <View style={styles.container}>
      {label ? <Text style={styles.label}>{label.toUpperCase()}</Text> : null}
      <View
        style={[
          styles.inputWrapper,
          focused && styles.inputFocused,
          !!error && styles.inputError,
          !editable && styles.inputDisabled,
        ]}
      >
        <TextInput
          {...inputProps}
          editable={editable}
          secureTextEntry={hidden}
          placeholderTextColor={colors.outline}
          style={[styles.input, secure && styles.inputWithToggle]}
          onFocus={(e) => {
            setFocused(true);
            onFocus?.(e);
          }}
          onBlur={(e) => {
            setFocused(false);
            onBlur?.(e);
          }}
        />
        {secure ? (
          <Pressable
            onPress={() => setHidden((h) => !h)}
            style={styles.toggle}
            accessibilityRole="button"
            accessibilityLabel="Afficher ou masquer le mot de passe"
          >
            <Icon name={hidden ? 'visibility' : 'visibility-off'} size={sizes.iconMd} color={colors.onSurfaceVariant} />
          </Pressable>
        ) : null}
      </View>
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.xs,
  },
  label: {
    ...typography.labelMd,
    color: colors.onSurface,
  },
  inputWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.surfaceContainerLow,
    borderWidth: 1,
    borderColor: colors.surfaceContainerLow,
  },
  inputFocused: {
    borderColor: colors.primaryContainer,
  },
  inputError: {
    borderColor: colors.error,
  },
  inputDisabled: {
    opacity: 0.6,
  },
  input: {
    ...typography.bodyMd,
    flex: 1,
    minHeight: sizes.controlHeight,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    color: colors.onSurface,
  },
  inputWithToggle: {
    paddingRight: spacing.xs,
  },
  toggle: {
    alignSelf: 'stretch',
    justifyContent: 'center',
    paddingHorizontal: spacing.md,
  },
  error: {
    ...typography.bodySm,
    color: colors.error,
  },
});
