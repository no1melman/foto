import React from 'react';

import './header.scss';
import { useHistory } from 'react-router';

const Header = () => {
  const history = useHistory()
  return (
    <div className="header-container">
      <div className="header-title">
        <h1>Foto</h1>
      </div>

      <div className="header-menu">
        <div className="header-menuitem" onClick={() => history.push('/')}>
          <span>Gallery</span>
        </div>
        <div className="header-menuitem" onClick={() => history.push('/collections')}>
          <span>Collections</span>
        </div>
      </div>
    </div>
  );
};

export default Header;
