import { ReactNode } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, View, ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { colors, spacing } from '../../constants/theme';

interface ScreenContainerProps {
  children: ReactNode;
  /** En-tête fixe au-dessus du contenu défilant (ex. AppHeader). */
  header?: ReactNode;
  contentStyle?: ViewStyle;
}

export function ScreenContainer({ children, header, contentStyle }: ScreenContainerProps) {
  return (
    <SafeAreaView style={styles.safeArea} edges={['top', 'bottom']}>
      {header}
      <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView
          style={styles.flex}
          contentContainerStyle={[styles.content, contentStyle]}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View style={styles.flex}>{children}</View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.surface,
  },
  flex: {
    flex: 1,
  },
  content: {
    flexGrow: 1,
    paddingHorizontal: spacing.gutter,
    paddingTop: spacing.margin,
    paddingBottom: spacing.xl,
  },
});
