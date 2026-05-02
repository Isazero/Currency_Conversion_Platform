import { NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Button } from '@/components/ui/button';

export default function NavBar() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `text-sm font-medium transition-colors ${
      isActive ? 'text-blue-600' : 'text-gray-600 hover:text-gray-900'
    }`;

  return (
    <nav className="border-b bg-white px-6 py-3 flex items-center gap-6">
      <span className="font-semibold text-gray-900 mr-2">Currency Platform</span>
      <NavLink to="/convert" className={linkClass}>Convert</NavLink>
      <NavLink to="/rates" className={linkClass}>Rates</NavLink>
      <NavLink to="/history" className={linkClass}>History</NavLink>
      <div className="flex-1" />
      <Button variant="outline" size="sm" onClick={handleLogout}>
        Sign Out
      </Button>
    </nav>
  );
}
