import React, { useState, useEffect, type ReactNode } from 'react';
import { View, Text, ScrollView } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { color, shadow, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { BalanceHero } from '@/components/money/BalanceHero';
import { AssetList } from '@/components/money/AssetList';
import { PerformanceCard } from '@/components/money/PerformanceCard';
import { ErrorCard } from '@/components/primitives/ErrorCard';
import { Pill } from '@/components/primitives/Pill';
import { Icon, IconName } from '@/components/primitives/Icon';
import { PressableScale } from '@/components/primitives/PressableScale';
import { FadeIn } from '@/components/primitives/FadeIn';
import { usePortfolio } from '@/features/portfolio/usePortfolio';
import { useVisibleHoldings } from '@/features/portfolio/useVisibleHoldings';
import { useHistory } from '@/features/history/useHistory';
import { useHeldSymbols } from '@/features/wallets/useHeldSymbols';
import { useActivation } from '@/features/bootstrap';
import { useCora } from '@/features/cora/useCora';
import { usePrefs } from '@/features/prefs/usePrefs';
import { useBaseCurrency } from '@/features/prefs/useBaseCurrency';
import type { Range } from '@/components/chart/RangeToggle';
import type { HomeSection } from '@/features/prefs/prefs.types';
import { HomeSetupPrompt } from '@/components/home/HomeSetupPrompt';
const QUICK: { label: string; icon: IconName; route: string; gold?: boolean }[] = [
  { label: 'Convert', icon: 'convert', route: 'Convert', gold: true },
  { label: 'Send', icon: 'send', route: 'Send' },
  { label: 'Pay', icon: 'pay', route: 'Pay' },
  { label: 'Receive', icon: 'receive', route: 'Receive' },
];

export function HomeScreen({ navigation }: any) {
  const { data, isLoading, isError, refetch } = usePortfolio();
  const [range, setRange] = useState<Range>('1M');
  const heldSymbols = useHeldSymbols();
  const hist = useHistory(range, heldSymbols);
  const { setActivation } = useActivation();
  const { lineFor, enabled: coraOn } = useCora();
  const { prefs, ready } = usePrefs();
  const base = useBaseCurrency();
  const [hidden, setHidden] = useState(false);

  const { visible: visibleHoldings, hidden: hiddenHoldings } = useVisibleHoldings(
    data?.holdings,
    prefs.enabledCurrencies,
  );

  useFocusEffect(
    React.useCallback(() => {
      setActivation('shell');
      return () => {};
    }, [setActivation]),
  );

  useEffect(() => {
    if (ready) setHidden(prefs.valuesHiddenOnLaunch);
  }, [ready, prefs.valuesHiddenOnLaunch]);

  const onRangeChange = (r: Range) => {
    setActivation('chart');
    setRange(r);
  };

  const walletSeries = base.convertSeries(hist.data?.series.find((s) => s.symbol === 'WALLET')?.points);
  const up = (data?.change24h.pct ?? 0) >= 0;
  const assetCount = visibleHoldings.length + hiddenHoldings.length;
  const currencyCount = new Set([...visibleHoldings, ...hiddenHoldings].map((h) => h.symbol)).size;
  const chartStale = (hist.isFetching && !!hist.data) || base.ratesStale;
  const showCoraFooter = coraOn && prefs.homeVisible.cora;

  const section = (id: HomeSection): ReactNode => {
    if (!prefs.homeVisible[id]) return null;
    switch (id) {
      case 'cora':
        // Line moved to quiet footer — no hero-height Cora box on Home.
        return null;
      case 'balance':
        return isLoading ? (
          <BalanceHero.Skeleton />
        ) : isError ? (
          <ErrorCard onRetry={refetch} />
        ) : (
          <>
            <BalanceHero
              totalLabel={base.formatTotal(data!.total)}
              hidden={hidden}
              up={up}
              series={walletSeries}
              stale={chartStale}
              change={
                hidden
                  ? '••••'
                  : base.formatChange(data!.change24h.amount, data!.change24h.pct)
              }
            />
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: -4 }}>
              <Pill label={assetCount + ' assets'} />
              <Pill label={currencyCount + ' currencies'} />
              <Pill label="self-custodied" gold />
            </View>
          </>
        );
      case 'quickActions':
        return (
          <View
            style={{
              flexDirection: 'row',
              justifyContent: 'space-between',
              paddingHorizontal: 2,
              marginVertical: 10,
              paddingVertical: 8,
            }}
          >
            {QUICK.map((q) => (
              <PressableScale
                key={q.label}
                onPress={() => navigation.navigate(q.route)}
                style={{ alignItems: 'center', gap: 7 }}
              >
                <View
                  style={[
                    {
                      width: 54,
                      height: 54,
                      borderRadius: 16,
                      alignItems: 'center',
                      justifyContent: 'center',
                      backgroundColor: q.gold ? color.gold : color.surface,
                    },
                    q.gold ? shadow.gold : shadow.card,
                  ]}
                >
                  <Icon name={q.icon} size={21} color={q.gold ? color.ink : color.deepPurple} />
                </View>
                <Text style={{ fontSize: 12, fontWeight: '600', fontFamily: font.body, color: color.text }}>
                  {q.label}
                </Text>
              </PressableScale>
            ))}
          </View>
        );
      case 'performance':
        return (
          <PerformanceCard
            series={(hist.data?.series ?? []).map((s) => ({ label: s.label, points: s.points }))}
            loading={hist.isLoading && !hist.data}
            range={range}
            onRange={onRangeChange}
            stale={chartStale}
          />
        );
      case 'assets':
        return (
          <>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
              <Text style={{ fontSize: 15, fontWeight: '800', fontFamily: font.body, color: color.text }}>Your assets</Text>
              <PressableScale onPress={() => setHidden((h) => !h)} hitSlop={10}>
                <Icon name={hidden ? 'eye-off' : 'eye'} size={18} color={color.textSubtle} />
              </PressableScale>
            </View>
            {isLoading ? (
              <AssetList.Skeleton rows={4} />
            ) : isError ? null : (
              <AssetList
                holdings={visibleHoldings}
                hiddenHoldings={hiddenHoldings}
                hidden={hidden}
              />
            )}
          </>
        );
      default:
        return null;
    }
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }} testID="home-screen">
      <Header brand online rightIcon="bell" />
      <ScrollView contentContainerStyle={{ padding: 18, paddingBottom: 110, gap: 13 }} showsVerticalScrollIndicator={false}>
        <FadeIn>
          <HomeSetupPrompt onNavigateSend={() => navigation.navigate('Send')} />
          {prefs.homeOrder.map((id) => {
            const node = section(id);
            if (!node) return null;
            return (
              <View key={id} style={{ gap: 13 }}>
                {node}
              </View>
            );
          })}
          <View style={{ paddingTop: 18, gap: 6, alignItems: 'center' }}>
            {showCoraFooter ? (
              <Text
                style={{
                  textAlign: 'center',
                  fontSize: 11,
                  lineHeight: 16,
                  color: color.textSubtle,
                  fontFamily: font.body,
                  opacity: 0.55,
                  maxWidth: 280,
                }}
              >
                {isLoading ? lineFor('homeLoad') : lineFor('home')}
              </Text>
            ) : null}
            <Text
              style={{
                textAlign: 'center',
                fontSize: 10,
                color: color.textSubtle,
                fontFamily: font.mono,
                opacity: 0.45,
              }}
            >
              Securities coming soon — pay with stock.
            </Text>
          </View>
        </FadeIn>
      </ScrollView>
      <CoraAssistant screen="home" />
    </View>
  );
}
