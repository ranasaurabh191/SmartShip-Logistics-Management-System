import { Chip } from '@mui/material';

interface StatusBadgeProps {
  status: 'Draft' | 'Booked' | 'InTransit' | 'Delivered' | 'Cancelled' | string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  let colorClass = 'bg-slate-100 text-slate-700 border-slate-200';

  switch (status.toLowerCase()) {
    case 'draft':
      colorClass = 'bg-slate-100 text-slate-700 border-slate-200';
      break;
    case 'booked':
      colorClass = 'bg-blue-50 text-blue-700 border-blue-200';
      break;
    case 'intransit':
    case 'in transit':
      colorClass = 'bg-amber-50 text-amber-700 border-amber-200';
      break;
    case 'delivered':
      colorClass = 'bg-emerald-50 text-emerald-700 border-emerald-200';
      break;
    case 'cancelled':
      colorClass = 'bg-rose-50 text-rose-700 border-rose-200';
      break;
  }

  return (
    <Chip 
      label={status} 
      size="small" 
      className={`font-semibold border ${colorClass}`}
    />
  );
};
