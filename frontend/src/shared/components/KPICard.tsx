import { Card, CardContent, Typography, Box } from '@mui/material';
import SvgIcon from '@mui/material/SvgIcon';

interface KPICardProps {
  title: string;
  value: string | number;
  icon: typeof SvgIcon;
  trend?: {
    value: number;
    isPositive: boolean;
  };
  color?: 'primary' | 'success' | 'warning' | 'error' | 'info';
}

export const KPICard: React.FC<KPICardProps> = ({ title, value, icon: Icon, trend, color = 'primary' }) => {
  const colorMap = {
    primary: 'text-blue-500 bg-blue-50',
    success: 'text-emerald-500 bg-emerald-50',
    warning: 'text-amber-500 bg-amber-50',
    error: 'text-rose-500 bg-rose-50',
    info: 'text-cyan-500 bg-cyan-50',
  };

  return (
    <Card className="rounded-xl border-slate-200 shadow-sm hover:shadow-md transition-shadow">
      <CardContent className="p-6">
        <Box className="flex justify-between items-start">
          <Box>
            <Typography color="text.secondary" variant="subtitle2" sx={{ fontWeight: '600' }} className="uppercase tracking-wider">
              {title}
            </Typography>
            <Typography variant="h4" sx={{ fontWeight: 'bold' }} className="mt-2 text-slate-800">
              {value}
            </Typography>
          </Box>
          <Box className={`p-3 rounded-xl ${colorMap[color]}`}>
            <Icon fontSize="medium" />
          </Box>
        </Box>
        
        {trend && (
          <Box className="mt-4 flex items-center gap-1">
            <Typography 
              variant="body2" 
              sx={{ fontWeight: 'bold' }} 
              className={trend.isPositive ? 'text-emerald-600' : 'text-rose-600'}
            >
              {trend.isPositive ? '+' : '-'}{Math.abs(trend.value)}%
            </Typography>
            <Typography variant="caption" color="text.secondary">
              from last month
            </Typography>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};
