import { createTheme } from '@mui/material/styles';

export const muiTheme = createTheme({
  typography: {
    fontFamily: '"Inter", sans-serif',
  },
  palette: {
    mode: 'dark',
    primary: { main: '#e0001a' },
    secondary: { main: '#00c48c' },
    background: { default: '#0d0d0d', paper: '#141414' },
    text: { primary: '#d4d4d4', secondary: '#888888' },
  },
  shape: { borderRadius: 2 },
  components: {
    MuiCssBaseline: {
      styleOverrides: { body: { backgroundColor: '#0d0d0d', color: '#d4d4d4' } },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'uppercase',
          fontFamily: '"Rajdhani", sans-serif',
          fontWeight: 700,
          letterSpacing: '0.1em',
          boxShadow: 'none',
          borderRadius: 2,
          '&:hover': { boxShadow: 'none' },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          background: '#141414',
          border: '1px solid rgba(224,0,26,0.22)',
          borderRadius: 4,
          boxShadow: 'none',
          '&:hover': { boxShadow: '0 0 18px rgba(224,0,26,0.14)' },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: { borderBottom: '1px solid rgba(255,255,255,0.05)', color: '#d4d4d4' },
        head: { color: '#888888', fontFamily: '"Rajdhani", sans-serif', fontWeight: 600, letterSpacing: '0.12em', fontSize: '11px', textTransform: 'uppercase' as const },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        root: { '&:hover': { backgroundColor: 'rgba(224,0,26,0.04)' } },
      },
    },
    MuiChip: {
      styleOverrides: { root: { borderRadius: 2, fontFamily: '"Rajdhani", sans-serif', fontWeight: 600, letterSpacing: '0.08em', fontSize: '11px' } },
    },
    MuiInputBase: {
      styleOverrides: {
        root: {
          background: '#1a1a1a',
          borderRadius: 2,
          fontSize: '13px',
          color: '#d4d4d4',
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        notchedOutline: { borderColor: 'rgba(255,255,255,0.08)' },
        root: { '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: '#e0001a' }, '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: '#e0001a' } },
      },
    },
    MuiTabs: {
      styleOverrides: {
        indicator: { backgroundColor: '#e0001a' },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          fontFamily: '"Rajdhani", sans-serif',
          fontWeight: 700,
          letterSpacing: '0.08em',
          textTransform: 'uppercase' as const,
          color: '#888',
          '&.Mui-selected': { color: '#ffffff' },
        },
      },
    },
  },
});
