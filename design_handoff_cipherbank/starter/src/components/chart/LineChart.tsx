import React from 'react';
import { View } from 'react-native';
import Svg, { Path, Defs, LinearGradient, Stop, Circle, Line } from 'react-native-svg';
import { color } from '@/theme';
import { toPath, Point } from './chartMath';

/** Brand series palette — reuse across compare mode + legends. */
export const SERIES_COLORS = [color.gold, color.violet, color.green, '#B8860B', color.deepPurple, color.red];

interface Props {
  data: Point[];
  width?: number; height?: number;
  stroke?: string;
  fill?: boolean;         // gradient area under the line
  dot?: boolean;          // end dot
  baseline?: boolean;     // dashed zero/flat baseline
  min?: number; max?: number;
}

/** Single-series line. Small height => sparkline; large => full chart. Pure SVG, no deps beyond react-native-svg. */
export function LineChart({ data, width = 300, height = 96, stroke = color.gold, fill = true, dot = true, baseline = false, min, max }: Props) {
  const { line, area, pts } = toPath(data, width, height, 8, min, max);
  const gid = 'g' + Math.round(width) + Math.round(height) + stroke.replace('#', '');
  const end = pts[pts.length - 1];
  return (
    <View>
      <Svg width={width} height={height}>
        <Defs>
          <LinearGradient id={gid} x1="0" y1="0" x2="0" y2="1">
            <Stop offset="0" stopColor={stroke} stopOpacity={0.28} />
            <Stop offset="1" stopColor={stroke} stopOpacity={0} />
          </LinearGradient>
        </Defs>
        {baseline ? <Line x1={0} y1={height - 8} x2={width} y2={height - 8} stroke={color.hairline} strokeWidth={1} strokeDasharray="3 4" /> : null}
        {fill && area ? <Path d={area} fill={'url(#' + gid + ')'} /> : null}
        {line ? <Path d={line} stroke={stroke} strokeWidth={2.4} fill="none" strokeLinecap="round" strokeLinejoin="round" /> : null}
        {dot && end ? <Circle cx={end.x} cy={end.y} r={3.5} fill={stroke} /> : null}
      </Svg>
    </View>
  );
}

/** Tiny inline sparkline (asset rows, chips). */
export function Sparkline({ data, up, width = 64, height = 26 }: { data: Point[]; up: boolean; width?: number; height?: number }) {
  return <LineChart data={data} width={width} height={height} stroke={up ? color.green : color.red} fill={false} dot={false} />;
}
